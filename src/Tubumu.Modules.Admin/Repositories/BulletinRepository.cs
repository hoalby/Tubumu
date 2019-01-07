﻿using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Tubumu.Modules.Admin.Entities;
using Tubumu.Modules.Admin.Models.Input;
using Tubumu.Modules.Framework.Extensions.Object;
using XM = Tubumu.Modules.Admin.Models;

namespace Tubumu.Modules.Admin.Repositories
{
    /// <summary>
    /// IBulletinRepository
    /// </summary>
    public interface IBulletinRepository
    {
        /// <summary>
        /// GetItemAsync
        /// </summary>
        /// <returns></returns>
        Task<XM.Bulletin> GetItemAsync();

        /// <summary>
        /// BulletinInput
        /// </summary>
        /// <param name="bulletin"></param>
        /// <param name="modelState"></param>
        /// <returns></returns>
        Task<bool> SaveAsync(BulletinInput bulletin, ModelStateDictionary modelState);
    }

    /// <summary>
    /// BulletinRepository
    /// </summary>
    public class BulletinRepository : IBulletinRepository
    {
        private readonly TubumuContext _tubumuContext;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tubumuContext"></param>
        public BulletinRepository(TubumuContext tubumuContext)
        {
            _tubumuContext = tubumuContext;
        }

        /// <summary>
        /// GetItemAsync
        /// </summary>
        /// <returns></returns>
        public async Task<XM.Bulletin> GetItemAsync()
        {
            var item = await _tubumuContext.Bulletin.AsNoTracking().FirstOrDefaultAsync();
            return item.MapTo<XM.Bulletin>();
        }

        /// <summary>
        /// SaveAsync
        /// </summary>
        /// <param name="bulletin"></param>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsync(BulletinInput bulletin, ModelStateDictionary modelState)
        {
            var dbBulletin = await _tubumuContext.Bulletin.FirstOrDefaultAsync();
            if (dbBulletin == null) return false;

            dbBulletin.UpdateFrom(bulletin);
            await _tubumuContext.SaveChangesAsync();

            return true;
        }
    }
}
