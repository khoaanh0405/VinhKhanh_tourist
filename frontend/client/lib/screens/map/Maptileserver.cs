using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using SQLite;

namespace client.lib.screens.map;

public sealed class MapTileServer : IDisposable
{
    // ── state ──────────────────────────────────────────────
    private readonly HttpListener _listener;
    private readonly string _mbtilesPath;
    private readonly string _patchedStyle;
    private readonly CancellationTokenSource _cts = new();

    // ── public ─────────────────────────────────────────────
    public int Port { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}";

    // ── constructor ────────────────────────────────────────
    public MapTileServer(string mbtilesPath, string rawStyleJson)
    {
        _mbtilesPath = mbtilesPath;
        Port = FindFreePort();
        _patchedStyle = PatchStyle(rawStyleJson);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        _listener.Prefixes.Add($"http://localhost:{Port}/");
    }

    // ── lifecycle ──────────────────────────────────────────
    public void Start()
    {
        _listener.Start();
        _ = ServeLoopAsync(_cts.Token);
        Debug.WriteLine($"[MapTileServer] Listening on {BaseUrl}");
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Close(); } catch { /* ignore */ }
    }

    // ── private: server loop ───────────────────────────────
    private async Task ServeLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = HandleAsync(ctx, ct);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MapTileServer] Loop error: {ex.Message}");
            }
        }
    }

    // ── private: request router ────────────────────────────
    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        // CORS – MapLibre fetches tiles cross-origin from the same localhost
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "*";

        try
        {
            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.StatusCode = 204;
                return;
            }

            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (path is "/" or "/index.html")
                await ServeTextAsync(ctx, GetMapHtml(), "text/html");

            else if (path == "/style.json")
                await ServeTextAsync(ctx, _patchedStyle, "application/json");

            else if (path.StartsWith("/tiles/"))
                await ServeTileAsync(ctx, path, ct);

            // Empty sprite – prevents MapLibre 404-spam when offline
            else if (path == "/sprite.json")
                await ServeTextAsync(ctx, "{}", "application/json");

            else if (path == "/sprite.png")
            {
                // 1×1 transparent PNG
                var png1x1 = Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
                ctx.Response.ContentType = "image/png";
                ctx.Response.ContentLength64 = png1x1.Length;
                await ctx.Response.OutputStream.WriteAsync(png1x1, ct);
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapTileServer] Handle error: {ex.Message}");
            try { ctx.Response.StatusCode = 500; } catch { /* ignore */ }
        }
        finally
        {
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    // ── tile serving ───────────────────────────────────────
    private async Task ServeTileAsync(
        HttpListenerContext ctx, string path, CancellationToken ct)
    {
        // path = /tiles/{z}/{x}/{y}.pbf
        var parts = path.TrimStart('/').Split('/');
        if (parts.Length != 4
            || !int.TryParse(parts[1], out int z)
            || !int.TryParse(parts[2], out int x)
            || !int.TryParse(parts[3].Replace(".pbf", ""), out int y))
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        var data = await Task.Run(() => ReadTile(z, x, y), ct);

        if (data is null || data.Length == 0)
        {
            // 204 No Content = valid empty tile
            ctx.Response.StatusCode = 204;
            return;
        }

        // MBTiles PBF tiles are already gzip-compressed
        ctx.Response.ContentType = "application/x-protobuf";
        ctx.Response.Headers["Content-Encoding"] = "gzip";
        ctx.Response.ContentLength64 = data.Length;
        await ctx.Response.OutputStream.WriteAsync(data, ct);
    }

    private byte[]? ReadTile(int z, int x, int y)
    {
        try
        {
            using var conn = new SQLiteConnection(_mbtilesPath, SQLiteOpenFlags.ReadOnly);

            // MBTiles uses TMS (bottom-origin) Y. XYZ→TMS: tms_y = (2^z − 1) − y
            int tmsY = (1 << z) - 1 - y;

            var cmd = conn.CreateCommand(
                "SELECT tile_data FROM tiles " +
                "WHERE zoom_level=? AND tile_column=? AND tile_row=?",
                z, x, tmsY);

            return cmd.ExecuteScalar<byte[]>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapTileServer] ReadTile z={z} x={x} y={y}: {ex.Message}");
            return null;
        }
    }

    // ── style.json patching ────────────────────────────────
    private string PatchStyle(string raw)
    {
        var doc = JsonNode.Parse(raw)!;

        // Replace online tile source → local tile server
        doc["sources"]!["openmaptiles"] = JsonNode.Parse($@"{{
            ""type"": ""vector"",
            ""tiles"": [""{BaseUrl}/tiles/{{z}}/{{x}}/{{y}}.pbf""],
            ""minzoom"": 0,
            ""maxzoom"": 14
        }}")!;

        // Offline-safe sprite (served locally)
        doc["sprite"] = $"{BaseUrl}/sprite";

        // NOTE: Glyphs (text labels) require internet OR a bundled font package.
        // For fully offline labels, download glyph PBFs and serve them locally.
        // For now we leave the CDN URL; map geometry renders fine without it.
        // doc["glyphs"] = "your-local-glyph-url";

        return doc.ToJsonString();
    }

    // ── MapLibre GL HTML page ──────────────────────────────
    private string GetMapHtml() => $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<meta name=""viewport""
      content=""width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no""/>
<link rel=""stylesheet""
      href=""https://unpkg.com/maplibre-gl@4.1.2/dist/maplibre-gl.css""/>
<script src=""https://unpkg.com/maplibre-gl@4.1.2/dist/maplibre-gl.js""></script>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  html, body, #map {{ width:100%; height:100%; overflow:hidden; background:#e8e0d8; }}
</style>
</head>
<body>
<div id=""map""></div>
<script>
// ── Init ──────────────────────────────────────────────────
const map = new maplibregl.Map({{
    container : 'map',
    style     : '{BaseUrl}/style.json',
    center    : [106.7044, 10.7607],   // Quận 4, HCM
    zoom      : 14,
    attributionControl : false,
    // Skip failing glyph requests gracefully
    transformRequest: (url, resourceType) => {{
        return {{ url, headers: {{}} }};
    }}
}});

map.addControl(new maplibregl.NavigationControl(), 'bottom-right');

// ── User-location marker ───────────────────────────────────
let _userMarker = null;

window.setUserLocation = function(lng, lat) {{
    const lngLat = [parseFloat(lng), parseFloat(lat)];
    if (!_userMarker) {{
        const el = Object.assign(document.createElement('div'), {{
            style: 'width:18px;height:18px;border-radius:50%;' +
                   'background:#4285F4;border:3px solid #fff;' +
                   'box-shadow:0 2px 6px rgba(0,0,0,.45);'
        }});
        _userMarker = new maplibregl.Marker({{ element: el }})
            .setLngLat(lngLat).addTo(map);
    }} else {{
        _userMarker.setLngLat(lngLat);
    }}
}};

// ── Camera ────────────────────────────────────────────────
window.moveCamera = function(lng, lat, zoom) {{
    map.flyTo({{ center: [parseFloat(lng), parseFloat(lat)],
                 zoom: parseFloat(zoom) || 15, duration: 700 }});
}};

// ── Route polyline ────────────────────────────────────────
window.drawRoute = function(coordsJson) {{
    const coords = JSON.parse(coordsJson);
    const gj = {{
        type: 'Feature', properties: {{}},
        geometry: {{ type: 'LineString', coordinates: coords }}
    }};
    if (map.getSource('route')) {{
        map.getSource('route').setData(gj);
    }} else {{
        map.addSource('route', {{ type: 'geojson', data: gj }});
        map.addLayer({{
            id: 'route-line', type: 'line', source: 'route',
            layout: {{ 'line-join': 'round', 'line-cap': 'round' }},
            paint: {{ 'line-color': '#4285F4', 'line-width': 5, 'line-opacity': 0.92 }}
        }});
        map.addLayer({{
            id: 'route-border', type: 'line', source: 'route',
            layout: {{ 'line-join': 'round', 'line-cap': 'round' }},
            paint: {{ 'line-color': '#1a5fb4', 'line-width': 8, 'line-opacity': 0.3 }},
            before: 'route-line'
        }});
    }}
}};

window.clearRoute = function() {{
    if (map.getSource('route')) {{
        map.getSource('route').setData({{
            type: 'Feature', properties: {{}},
            geometry: {{ type: 'LineString', coordinates: [] }}
        }});
    }}
}};

// ── POI markers ───────────────────────────────────────────
window.addPoiMarkers = function(poisJson) {{
    JSON.parse(poisJson).forEach(poi => {{
        const el = Object.assign(document.createElement('div'), {{
            textContent: '📍',
            title      : poi.name,
            style      : 'font-size:22px;cursor:pointer;user-select:none;'
        }});
        el.onclick = () => {{
            window.location.href = 'mauiapp://poi-selected?id=' + poi.id;
        }};
        new maplibregl.Marker({{ element: el }})
            .setLngLat([poi.lng, poi.lat])
            .setPopup(new maplibregl.Popup({{offset:25}})
                .setHTML('<b>' + poi.name + '</b>'))
            .addTo(map);
    }});
}};
</script>
</body>
</html>";

    // ── utility ────────────────────────────────────────────
    private static async Task ServeTextAsync(
        HttpListenerContext ctx, string text, string mime)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.ContentType = $"{mime}; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
    }

    private static int FindFreePort()
    {
        var probe = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        probe.Start();
        int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}