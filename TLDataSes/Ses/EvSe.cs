
using TLMos.Mos;
using TLDataRes.Res;
using TLDataSes.Ins;
using TLDataRes.Ins;
using System.ComponentModel;
using System.Collections.Generic;
using TLMos.DTOs;

namespace TLDataSes.Ses
{
    public class EvSe: IEvSe
    {
        private readonly IEvRe _XRepo;
        public EvSe(IEvRe XRepo) {
            _XRepo = XRepo;
        }
        public async Task<List<Ev>> GetEvs()
        {
            List<Ev> Events = await _XRepo.GetEvs();
            return Events;
        }
        public async Task<ResultOptions> IuEv(Ev ev)
        {
            return await _XRepo.IuEv(ev);
        }

    }
}
