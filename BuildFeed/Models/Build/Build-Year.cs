﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BuildFeed.Models
{
   public partial class Build
   {
      public async Task<int[]> SelectAllYears(int limit = -1, int skip = 0)
      {
         var query = _buildCollection.Aggregate()
                                     .Group(new BsonDocument("_id", $"${nameof(BuildModel.BuildTime)}.{nameof(DateTime.Year)}"))
                                     .Sort(new BsonDocument("_id", -1))
                                     .Skip(skip);

         if (limit > 0)
         {
            query = query.Limit(limit);
         }

         var grouping = await query.ToListAsync();

         return (from g in grouping
                 select g["_id"].AsInt32).ToArray();
      }

      public async Task<long> SelectAllYearsCount()
      {
         var query = await _buildCollection.Aggregate()
                                           .Group(new BsonDocument("_id", $"${nameof(BuildModel.BuildTime)}.{nameof(DateTime.Year)}"))
                                           .ToListAsync();

         return query.Count;
      }

      public async Task<List<BuildModel>> SelectYear(int year, int limit = -1, int skip = 0)
      {
         var query = _buildCollection.Find(Builders<BuildModel>.Filter.And(Builders<BuildModel>.Filter.Gte(b => b.BuildTime,
                                                                                                           new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                                                                           Builders<BuildModel>.Filter.Lte(b => b.BuildTime,
                                                                                                           new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc))))
                                     .Sort(sortByDate)
                                     .Skip(skip);

         if (limit > 0)
         {
            query = query.Limit(limit);
         }

         return await query.ToListAsync();
      }

      public async Task<long> SelectYearCount(int year)
      {
         return await _buildCollection.CountAsync(Builders<BuildModel>.Filter.And(Builders<BuildModel>.Filter.Gte(b => b.BuildTime,
                                                                                                                  new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                                                                                  Builders<BuildModel>.Filter.Lte(b => b.BuildTime,
                                                                                                                  new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc))));
      }
   }
}