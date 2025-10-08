using SuperAppModels.Mos;
using SuperAppDataRepositories.Repositories;
using SuperAppDataServices.Ins;
using SuperAppDataRepositories.Ins;
using System.ComponentModel;
using System.Collections.Generic;
using SuperAppModels.DTOs;
using Microsoft.AspNetCore.Http;
using System;

namespace SuperAppDataServices.Services
{
    public class StandardRegistryService: IStandardRegistryService
    {
        private readonly IStandardRegistryRepository _XRepo;
        
        public StandardRegistryService(IStandardRegistryRepository XRepo)
        {
            _XRepo = XRepo;
        }
        
        public async Task<List<StandardRegistry>> GetStandardRegistries(string? type)
        {
            List<StandardRegistry> sr = await _XRepo.GetStandardRegistries(type);
            return sr;
        }
    }
}
