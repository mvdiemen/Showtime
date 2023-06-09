﻿using Microsoft.EntityFrameworkCore;
using Polly;
using Showtime.ApplicationServices;
using Showtime.Domain;

namespace Showtime.Infrastructure.Datastorage;
public class ShowtimeRepository : ISyncStatusRepository, IShowRepository
{
    private readonly ShowDbContext _dbContext;

    public ShowtimeRepository(ShowDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext, nameof(dbContext));

        _dbContext = dbContext;
    }

    public async Task AddPage(int pagenumber)
    {
        var syncStatus = await _dbContext.SyncStatus.FirstOrDefaultAsync(s => s.Pagenumber == pagenumber);

        if (syncStatus is not null)
        {
            syncStatus.ResetDateProcessed();
            _dbContext.SyncStatus.Update(syncStatus);
        }
        else
        {
            var syncStatusToAdd = new SyncStatus(pagenumber);
            _dbContext.SyncStatus.Add(syncStatusToAdd);
        }
    }

    public void AddShow(Show show)
    {
        _dbContext.Shows.Add(show);
    }

    public async Task<int> GetLastStoredPage()
    {
        var lastPage = await _dbContext.SyncStatus.OrderByDescending(p => p.Pagenumber).FirstOrDefaultAsync();
        return lastPage != null ? lastPage.Pagenumber : 0;
    }

    public Task<bool> ShowExistsAsync(int id)
    {
        return _dbContext.Shows.AnyAsync(s => s.Id == id);
    }

    //Used keyset pagination as described: https://learn.microsoft.com/en-us/ef/core/querying/pagination
    public async Task<IEnumerable<Show>> FindAllShows(int lastId)
    {
        var shows = await _dbContext.Shows
            .OrderBy(b => b.Id)
            .Where(b => b.Id > lastId)
            .Take(100)
            .ToListAsync();

        return shows;
    }
}
