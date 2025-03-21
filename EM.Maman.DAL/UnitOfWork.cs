﻿using EM.Maman.Models.Interfaces;
using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL
{
  
        public class UnitOfWork : IUnitOfWork
        {
            private readonly LocalMamanDBContext _context;
            private bool _disposed = false;

            public ITrolleyRepository Trolleys { get; }
            public ICellRepository Cells { get; }
            public IFingerRepository Fingers { get; }
            public ITaskRepository Tasks { get; }
            public IOperationRepository Operations { get; }
            public IPalletRepository Pallets { get; }
            public IUserRepository Users { get; }

            public UnitOfWork(
                LocalMamanDBContext context,
                ITrolleyRepository trolleyRepository,
                ICellRepository cellRepository,
                IFingerRepository fingerRepository,
                ITaskRepository taskRepository,
                IOperationRepository operationRepository,
                IPalletRepository palletRepository,
                IUserRepository userRepository)
            {
                _context = context;
                Trolleys = trolleyRepository;
                Cells = cellRepository;
                Fingers = fingerRepository;
                Tasks = taskRepository;
                Operations = operationRepository;
                Pallets = palletRepository;
                Users = userRepository;
            }

            public async Task<int> CompleteAsync()
            {
                return await _context.SaveChangesAsync();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _context.Dispose();
                    }
                    _disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }

