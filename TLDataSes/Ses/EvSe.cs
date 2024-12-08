
using TLMos.Mos;
using TLDataRes.Res;
using TLDataSes.Ins;
using TLDataRes.Ins;
using System.ComponentModel;
using System.Collections.Generic;
using TLMos.DTOs;
using Microsoft.AspNetCore.Http;
using System;

namespace TLDataSes.Ses
{
    public class EvSe: IEvSe
    {
        private readonly IEvRe _XRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public EvSe(IEvRe XRepo, IHttpContextAccessor httpContextAccessor)
        {
            _XRepo = XRepo;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<List<Ev>> GetEvs()
        {
            List<Ev> Events = await _XRepo.GetEvs();
           
            // convert UTC time to UserLocalTime
            string UserTimeZone = _httpContextAccessor.HttpContext.Request.Headers["X-TimeZone"];
            var userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(UserTimeZone);
            foreach (var ev in Events)
            {
                ev.TimeStart = TimeZoneInfo.ConvertTimeFromUtc(ev.TimeStart, userTimeZoneInfo);
                if (ev.TimeEnd.HasValue)
                    ev.TimeEnd = TimeZoneInfo.ConvertTimeFromUtc(ev.TimeEnd.Value, userTimeZoneInfo);
            }

            return Events;
        }
        public async Task<ResultOptions> IuEv(Ev ev)
        {
            //string UserTimeZone = _httpContextAccessor.HttpContext.Request.Headers["X-TimeZone"];
            //var userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(UserTimeZone);
            //ev.TimeStart = TimeZoneInfo.ConvertTimeToUtc(ev.TimeStart, userTimeZoneInfo);
            //if (ev.TimeEnd.HasValue)
            //    ev.TimeEnd = TimeZoneInfo.ConvertTimeToUtc(ev.TimeEnd.Value, userTimeZoneInfo);

            return await _XRepo.IuEv(ev);
        }

    }
}
