using SQLite;
using client.lib.core;
using client.lib.model;

namespace client.lib.services;

public class LocalDbService
{
    private SQLiteAsyncConnection _db;

    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task InitAsync()
    {
        if (_db is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return;

            _db = new SQLiteAsyncConnection(DatabaseConstants.DatabasePath, DatabaseConstants.Flags);

            await _db.CreateTableAsync<PoiLocal>();
            await _db.CreateTableAsync<RestaurantLocal>();
            await _db.CreateTableAsync<FoodLocal>();
            await _db.CreateTableAsync<NarrationLocal>();
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task ClearAllDataAsync()
    {
        await InitAsync();
        await _db.DeleteAllAsync<FoodLocal>();
        await _db.DeleteAllAsync<RestaurantLocal>();
        await _db.DeleteAllAsync<PoiLocal>();
        // 🔥 THÊM DÒNG XÓA NARRATIONS ĐỂ ĐẢM BẢO CLEAR SẠCH DATA CŨ
        await _db.DeleteAllAsync<NarrationLocal>();
    }

    public async Task SavePoiDataAsync(PoiLocal poi, List<RestaurantLocal> restaurants, List<FoodLocal> foods, List<NarrationLocal> narrations)
    {
        await InitAsync();
        await _db.InsertOrReplaceAsync(poi);

        if (restaurants != null && restaurants.Any())
            await _db.InsertAllAsync(restaurants);

        if (foods != null && foods.Any())
            await _db.InsertAllAsync(foods);

        // 🔥 THỰC HIỆN LƯU NARRATIONS VÀO SQLITE
        if (narrations != null && narrations.Any())
            await _db.InsertAllAsync(narrations);
    }

    public async Task<List<PoiLocal>> GetAllPoisAsync()
    {
        await InitAsync();
        return await _db.Table<PoiLocal>().ToListAsync();
    }

    public async Task<PoiLocal> GetPoiByIdAsync(int poiId)
    {
        await InitAsync();
        return await _db.Table<PoiLocal>().Where(p => p.PoiId == poiId).FirstOrDefaultAsync();
    }

    public async Task<List<RestaurantLocal>> GetRestaurantsByPoiIdAsync(int poiId)
    {
        await InitAsync();
        return await _db.Table<RestaurantLocal>().Where(r => r.PoiId == poiId).ToListAsync();
    }

    public async Task<List<FoodLocal>> GetFoodsByRestaurantIdAsync(int restaurantId)
    {
        await InitAsync();
        return await _db.Table<FoodLocal>().Where(f => f.RestaurantId == restaurantId).ToListAsync();
    }

    public async Task<List<NarrationLocal>> GetNarrationsByPoiIdAsync(int poiId)
    {
        await InitAsync();
        return await _db.Table<NarrationLocal>().Where(n => n.PoiId == poiId).ToListAsync();
    }
}