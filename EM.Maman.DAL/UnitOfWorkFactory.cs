using EM.Maman.Models.Interfaces;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbContextFactory<LocalMamanDBContext> _contextFactory;
        private readonly ILoggerFactory _loggerFactory;

        public UnitOfWorkFactory(
            IDbContextFactory<LocalMamanDBContext> contextFactory,
            ILoggerFactory loggerFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IUnitOfWork CreateUnitOfWork()
        {
            var logger = _loggerFactory.CreateLogger<UnitOfWork>();
            return new UnitOfWork(_contextFactory, logger);
        }

        public async Task<IUnitOfWork> CreateUnitOfWorkAsync()
        {
            // Create the unit of work asynchronously if needed
            return await System.Threading.Tasks.Task.FromResult(CreateUnitOfWork());
        }
    }
}
