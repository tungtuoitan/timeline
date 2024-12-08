
using TLMos.Mos;
using TLDataRes.Res;
using TLDataSes.Ins;
using TLDataRes.Ins;
using System.ComponentModel;
using System.Collections.Generic;
using TLMos.DTOs;

namespace TLDataSes.Ses
{
    public class XSe: IXSe
    {
        private readonly IXRe _XRepo;
        public XSe(IXRe XRepo) {
            _XRepo = XRepo;
        }
        public async Task<List<Event>> GetEvents()
        {
            List<Event> Events = await _XRepo.GetEvents();
            return Events;
        }
        public async Task<ResultOptions> IuEv(Event ev)
        {
            return await _XRepo.IuEv(ev);
        }

    }
}
