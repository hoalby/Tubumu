﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Tubumu.Modules.Admin.Entities;
using Tubumu.Modules.Admin.Models.Input;
using Tubumu.Modules.Framework.Extensions;
using Tubumu.Modules.Framework.Utilities.Cryptography;
using Tubumu.Modules.Framework.Models;
using XM = Tubumu.Modules.Admin.Models;

namespace Tubumu.Modules.Admin.Repositories
{
    public interface IMobileUserRepository
    {
        Task<bool> IsExistsMobileAsync(string mobile);
        Task<bool> IsExistsMobilesAsync(IEnumerable<string> mobiles);
        Task<bool> VerifyExistsMobileAsync(int userId, string mobile);
        Task<bool> ChangeMobileAsync(int userId, string newMobile, bool mobileIsValid, ModelStateDictionary modelState);
        Task<XM.UserInfo> GenerateItemAsync(Guid groupId, XM.UserStatus status, string mobile, string password, ModelStateDictionary modelState);
        Task<int> ResetPasswordAsync(string mobile, string password, ModelStateDictionary modelState);
        Task<XM.UserInfo> GetItemByMobileAsync(string mobile, bool mobileIsValid = true, XM.UserStatus? status = null);
        Task<XM.UserInfo> GetOrGenerateItemByMobileAsync(Guid groupId, XM.UserStatus generateStatus, string mobile, bool mobileIsValid);
    }

    public class MobileUserRepository : IMobileUserRepository
    {
        private readonly Expression<Func<User, XM.UserInfo>> _selector;

        private readonly TubumuContext _tubumuContext;

        public MobileUserRepository(TubumuContext tubumuContext)
        {
            _tubumuContext = tubumuContext;

            _selector = u => new XM.UserInfo
            {
                UserId = u.UserId,
                Username = u.Username,
                DisplayName = u.DisplayName,
                LogoUrl = u.LogoUrl,
                RealName = u.RealName,
                RealNameIsValid = u.RealNameIsValid,
                Email = u.Email,
                EmailIsValid = u.EmailIsValid,
                Mobile = u.Mobile,
                MobileIsValid = u.MobileIsValid,
                Password = u.Password,
                WeixinMobileOpenId = u.WeixinMobileOpenId,
                WeixinAppOpenId = u.WeixinAppOpenId,
                WeixinWebOpenId = u.WeixinWebOpenId,
                WeixinUnionId = u.WeixinUnionId,
                CreationDate = u.CreationDate,
                Description = u.Description,
                Status = u.Status,
                HeadUrl = u.HeadUrl,
                IsDeveloper = u.IsDeveloper,
                IsTester = u.IsTester,
                Group = new XM.GroupInfo
                {
                    GroupId = u.Group.GroupId,
                    Name = u.Group.Name,
                },
                Groups = from ur in u.UserGroup
                         select new XM.GroupInfo
                         {
                             GroupId = ur.GroupId,
                             Name = ur.Group.Name,
                         },
                Role = new XM.RoleInfo
                {
                    RoleId = u.Role != null ? u.Role.RoleId : Guid.Empty,
                    Name = u.Role != null ? u.Role.Name : String.Empty,
                },
                Roles = from ur in u.UserRole
                        select new XM.RoleInfo
                        {
                            RoleId = ur.RoleId,
                            Name = ur.Role.Name,
                        },
                GroupRoles = from ugr in u.Group.GroupRole
                             select new XM.RoleInfo
                             {
                                 RoleId = ugr.RoleId,
                                 Name = ugr.Role.Name,
                             },
                GroupsRoles =
                    from ug in u.UserGroup
                    from ugr in ug.Group.GroupRole
                    select new XM.RoleBase
                    {
                        RoleId = ugr.RoleId,
                        Name = ugr.Role.Name,
                        IsSystem = ugr.Role.IsSystem,
                        DisplayOrder = ugr.Role.DisplayOrder
                    },
                Permissions = from up in u.UserPermission
                              select new XM.PermissionBase
                              {
                                  ModuleName = up.Permission.ModuleName,
                                  PermissionId = up.PermissionId,
                                  Name = up.Permission.Name
                              },
                GroupPermissions = from upp in u.Group.GroupPermission
                                   select new XM.PermissionBase
                                   {
                                       ModuleName = upp.Permission.ModuleName,
                                       PermissionId = upp.PermissionId,
                                       Name = upp.Permission.Name
                                   },
                GroupsPermissions = from gs in u.UserGroup
                                    from upp in gs.Group.GroupPermission
                                    select new XM.PermissionBase
                                    {
                                        ModuleName = upp.Permission.ModuleName,
                                        PermissionId = upp.PermissionId,
                                        Name = upp.Permission.Name
                                    },
                RolePermissions = from ur in u.Role.RolePermission
                                  where u.Role != null
                                  select new XM.PermissionBase
                                  {
                                      ModuleName = ur.Permission.ModuleName,
                                      PermissionId = ur.PermissionId,
                                      Name = ur.Permission.Name
                                  },
                RolesPermissions = from usr in u.UserRole
                                   from urp in usr.Role.RolePermission
                                   select new XM.PermissionBase
                                   {
                                       ModuleName = urp.Permission.ModuleName,
                                       PermissionId = urp.PermissionId,
                                       Name = urp.Permission.Name
                                   },
                GroupRolesPermissions = from gr in u.Group.GroupRole
                                        from p in gr.Role.RolePermission
                                        select new XM.PermissionBase
                                        {
                                            ModuleName = p.Permission.ModuleName,
                                            PermissionId = p.PermissionId,
                                            Name = p.Permission.Name
                                        },
                GroupsRolesPermissions =
                    from ug in u.UserGroup
                    from usr in ug.Group.GroupRole
                    from urp in usr.Role.RolePermission
                    select new XM.PermissionBase
                    {
                        ModuleName = urp.Permission.ModuleName,
                        PermissionId = urp.PermissionId,
                        Name = urp.Permission.Name
                    },
            };
        }

        public async Task<bool> IsExistsMobileAsync(string mobile)
        {
            if (mobile.IsNullOrWhiteSpace()) return false;
            return await _tubumuContext.User.AnyAsync(m => m.Mobile == mobile);
        }

        public async Task<bool> IsExistsMobilesAsync(IEnumerable<string> mobiles)
        {
            var enumerable = mobiles as string[] ?? mobiles.ToArray();
            if (enumerable.Length == 0) return false;
            return await _tubumuContext.User.Where(m => mobiles.Contains(m.Mobile)).AnyAsync();
        }

        public async Task<bool> VerifyExistsMobileAsync(int userId, string mobile)
        {
            if (mobile.IsNullOrWhiteSpace()) return false;
            return await _tubumuContext.User.AnyAsync(m => m.UserId != userId && m.Mobile == mobile);
        }

        public async Task<bool> ChangeMobileAsync(int userId, string newMobile, bool mobileIsValid, ModelStateDictionary modelState)
        {
            var user = await _tubumuContext.User.FirstOrDefaultAsync(m => m.UserId == userId);
            if (user == null)
            {
                modelState.AddModelError("UserId", "当前用户不存在");
                return false;
            }
            if (!user.Mobile.IsNullOrWhiteSpace() &&
                user.Mobile.Equals(newMobile, StringComparison.InvariantCultureIgnoreCase))
            {
                modelState.AddModelError("UserId", "目标手机号和当前手机号相同");
                return false;
            }
            if (_tubumuContext.User.Any(m => m.UserId != userId && m.Mobile == newMobile))
            {
                modelState.AddModelError("UserId", "手机号[{0}]已经被使用".FormatWith(newMobile));
                return false;
            }
            user.MobileIsValid = mobileIsValid;
            user.Mobile = newMobile;
            await _tubumuContext.SaveChangesAsync();

            return true;
        }

        public async Task<XM.UserInfo> GenerateItemAsync(Guid groupId, XM.UserStatus status, string mobile, string password, ModelStateDictionary modelState)
        {
            if(await _tubumuContext.User.AnyAsync(m => m.Mobile == mobile))
            {
                modelState.AddModelError(nameof(mobile), $"手机号 {mobile} 已被注册。");
                return null;
            }

            var newUser = new User
            {
                Status = status,
                CreationDate = DateTime.Now,
                Mobile = mobile,
                MobileIsValid = true,
                GroupId = groupId,
                Username = "g" + Guid.NewGuid().ToString("N").Substring(19),
                Password = password,
            };

            _tubumuContext.User.Add(newUser);
            await _tubumuContext.SaveChangesAsync();
            return await _tubumuContext.User.AsNoTracking().Select(_selector).FirstOrDefaultAsync(m => m.UserId == newUser.UserId);
        }

        public async Task<int> ResetPasswordAsync(string mobile, string password, ModelStateDictionary modelState)
        {
            if(!await _tubumuContext.User.AnyAsync(m => m.Mobile == mobile))
            {
                modelState.AddModelError(nameof(mobile), $"手机号 {mobile} 尚未注册。");
                return 0;
            }

            var user = await _tubumuContext.User.Where(m=>m.Mobile == mobile).FirstOrDefaultAsync();
            if(user == null)
            {
                modelState.AddModelError(nameof(mobile), $"手机号 {mobile} 尚未注册。");
                return 0;
            }
            if(user.Status != XM.UserStatus.Normal)
            {
                modelState.AddModelError(nameof(mobile), $"手机号 {mobile} 的用户状态不允许重置密码。");
                return 0;
            }

            user.Password = password;
            await _tubumuContext.SaveChangesAsync();
            return user.UserId;
        }

        public async Task<XM.UserInfo> GetItemByMobileAsync(string mobile, bool mobileIsValid = true, XM.UserStatus? status = null)
        {
            XM.UserInfo user;
            if (status.HasValue)
            {
                user = await _tubumuContext.User.AsNoTracking().Select(_selector).FirstOrDefaultAsync(m => (m.MobileIsValid && m.Mobile == mobile) && m.Status == status.Value);
            }
            else
            {
                user = await _tubumuContext.User.AsNoTracking().Select(_selector).FirstOrDefaultAsync(m => (m.MobileIsValid && m.Mobile == mobile));
            }

            return user;
        }

        public async Task<XM.UserInfo> GetOrGenerateItemByMobileAsync(Guid groupId, XM.UserStatus generateStatus, string mobile, bool mobileIsValid)
        {
            if (mobile.IsNullOrWhiteSpace()) return null;
            var user = await GetItemByMobileAsync(mobile);
            if (user == null)
            {
                var newUser = new User
                {
                    Status = generateStatus,
                    CreationDate = DateTime.Now,
                    Mobile = mobile,
                    MobileIsValid = mobileIsValid,
                    GroupId = groupId, // new Guid("11111111-1111-1111-1111-111111111111") 等待分配组
                    Username = "g" + Guid.NewGuid().ToString("N").Substring(19),
                    Password = mobile,
                };

                _tubumuContext.User.Add(newUser);
                await _tubumuContext.SaveChangesAsync();
            }
            return await GetItemByMobileAsync(mobile);
        }
    }
}
