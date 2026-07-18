using webHuyBeo.Models;
using webHuyBeo.Repositories;

namespace webHuyBeo.Services
{
    public interface IMenuService
    {
        Task<IEnumerable<DanhMuc>> GetAllDanhMucsAsync();
        Task<IEnumerable<MonAn>> GetAllMonAnsAsync(string? search = null, int? danhMucId = null);
        Task<MonAn?> GetMonAnByIdAsync(int id);
        Task<IEnumerable<Topping>> GetAllToppingsAsync(bool onlyAvailable = false);
        Task CreateMonAnAsync(MonAn monAn, int[]? toppingIds);
        Task UpdateMonAnAsync(MonAn monAn, int[]? toppingIds);
        Task DeleteMonAnAsync(int id);
    }

    public class MenuService : IMenuService
    {
        private readonly IRepository<DanhMuc> _danhMucRepo;
        private readonly IRepository<MonAn> _monAnRepo;
        private readonly IRepository<Topping> _toppingRepo;
        private readonly IRepository<MonAnTopping> _monAnToppingRepo;

        public MenuService(
            IRepository<DanhMuc> danhMucRepo,
            IRepository<MonAn> monAnRepo,
            IRepository<Topping> toppingRepo,
            IRepository<MonAnTopping> monAnToppingRepo)
        {
            _danhMucRepo = danhMucRepo;
            _monAnRepo = monAnRepo;
            _toppingRepo = toppingRepo;
            _monAnToppingRepo = monAnToppingRepo;
        }

        public async Task<IEnumerable<DanhMuc>> GetAllDanhMucsAsync()
        {
            var list = await _danhMucRepo.GetAllAsync();
            return list.OrderBy(d => d.ThuTu);
        }

        public async Task<IEnumerable<MonAn>> GetAllMonAnsAsync(string? search = null, int? danhMucId = null)
        {
            var query = _monAnRepo.Query(includeProperties: "DanhMuc");
            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.TenMon.Contains(search));
            if (danhMucId.HasValue)
                query = query.Where(m => m.DanhMucID == danhMucId);
            
            return query.OrderBy(m => m.DanhMucID).ThenBy(m => m.TenMon).ToList();
        }

        public async Task<MonAn?> GetMonAnByIdAsync(int id)
        {
            return await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id, includeProperties: "MonAnToppings");
        }

        public async Task<IEnumerable<Topping>> GetAllToppingsAsync(bool onlyAvailable = false)
        {
            if (onlyAvailable)
                return await _toppingRepo.GetAllAsync(t => t.TrangThai == "ConBan");
            return await _toppingRepo.GetAllAsync();
        }

        public async Task CreateMonAnAsync(MonAn monAn, int[]? toppingIds)
        {
            await _monAnRepo.AddAsync(monAn);
            await _monAnRepo.SaveAsync();

            if (toppingIds != null && toppingIds.Length > 0)
            {
                foreach (var toppingId in toppingIds)
                {
                    await _monAnToppingRepo.AddAsync(new MonAnTopping { MonAnID = monAn.MonAnID, ToppingID = toppingId });
                }
                await _monAnToppingRepo.SaveAsync();
            }
        }

        public async Task UpdateMonAnAsync(MonAn monAn, int[]? toppingIds)
        {
            _monAnRepo.Update(monAn);
            await _monAnRepo.SaveAsync();

            // Xóa topping cũ
            var existingToppings = await _monAnToppingRepo.GetAllAsync(t => t.MonAnID == monAn.MonAnID);
            _monAnToppingRepo.RemoveRange(existingToppings);
            await _monAnToppingRepo.SaveAsync();

            // Thêm topping mới
            if (toppingIds != null && toppingIds.Length > 0)
            {
                foreach (var toppingId in toppingIds)
                {
                    await _monAnToppingRepo.AddAsync(new MonAnTopping { MonAnID = monAn.MonAnID, ToppingID = toppingId });
                }
                await _monAnToppingRepo.SaveAsync();
            }
        }

        public async Task DeleteMonAnAsync(int id)
        {
            var monAn = await _monAnRepo.GetFirstOrDefaultAsync(m => m.MonAnID == id);
            if (monAn != null)
            {
                _monAnRepo.Remove(monAn);
                await _monAnRepo.SaveAsync();
            }
        }
    }
}
