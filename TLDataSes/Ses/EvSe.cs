
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
            List<Ev> evs = await _XRepo.GetEvs();
           
            // convert UTC time to UserLocalTime
            foreach (var ev in evs){
                ev.TimeStart = ConvertUTCToUserTimeZone(ev.TimeStart);
                if (ev.TimeEnd.HasValue)
                    ev.TimeEnd = ConvertUTCToUserTimeZone(ev.TimeEnd.Value);
            }

            return evs;
        }
        public async Task<EvsResult> IuEv(Ev ev)
        {
            var evResult = await _XRepo.IuEv(ev);

            // convert UTC time to UserLocalTime
            foreach (var _ev in evResult.Evs)
            {
                _ev.TimeStart = ConvertUTCToUserTimeZone(_ev.TimeStart);
                if (_ev.TimeEnd.HasValue)
                    _ev.TimeEnd = ConvertUTCToUserTimeZone(_ev.TimeEnd.Value);
            }

            return evResult;
        }
        public DateTime ConvertUTCToUserTimeZone(DateTime utcDateTime)
        {
            string userTimeZone = _httpContextAccessor.HttpContext.Request.Headers["X-TimeZone"];
            var userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(userTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, userTimeZoneInfo);
        }

    }
}
