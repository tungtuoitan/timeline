
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
    public class SRsSe: ISRsSe
    {
        private readonly ISRsRe _XRepo;
        public SRsSe(ISRsRe XRepo)
        {
            _XRepo = XRepo;
        }
        public async Task<List<SR>> GetSRs(string? type)
        {
            List<SR> sr = await _XRepo.GetSRs(type);
            return sr;
        }
    }
}
