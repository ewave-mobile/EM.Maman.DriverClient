﻿using EM.Maman.Models.CustomModels;
using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.DAL.Repositories
{
    public class CellRepository : Repository<Cell>, ICellRepository
    {
        public CellRepository(LocalMamanDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Cell>> GetCellsByPositionAsync(int position)
        {
            return await Context.Cells
                .Where(c => c.Position == position)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cell>> GetCellsBySideAsync(int side)
        {
            return await Context.Cells
                .Where(c => c.Side == side)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cell>> GetActiveOnlyCellsAsync()
        {
      // throw exception
      throw new NotImplementedException();
        }

        public async Task<Cell> GetCellWithPalletAsync(int cellId)
        {
            // Assuming you have relationships defined
            throw new NotImplementedException();
        }
        public async Task<IEnumerable<PalletInCell>> GetPalletsWithCellsAsync()
        {
            return await Context.PalletInCells
                .Include(pic => pic.PalletId)
                .Include(pic => pic.CellId)
                .ToListAsync();
        }
        public async Task<IEnumerable<CellWithPalletInfo>> GetCellsWithPalletsAsync()
        {
            // First get all cells
            var cells = await Context.Cells.ToListAsync();

            // Get all pallet assignments
            var palletAssignments = await Context.PalletInCells.ToListAsync();

            // Get all pallets
            var pallets = await Context.Pallets.ToListAsync();

            // Join the data to create CellWithPalletInfo objects
            var result = cells.Select(cell => {
                // Find if there's a pallet assigned to this cell
                var assignment = palletAssignments.FirstOrDefault(pic => pic.CellId == cell.Id);

                // If there is an assignment, find the corresponding pallet
                Pallet? pallet = null;
                if (assignment != null)
                {
                    pallet = pallets.FirstOrDefault(p => p.Id == assignment.PalletId);
                }

                // Create and return the combined object
                return new CellWithPalletInfo
                {
                    Cell = cell,
                    Pallet = pallet,
                };
            }).ToList();

            return result;
        }
    }
}
