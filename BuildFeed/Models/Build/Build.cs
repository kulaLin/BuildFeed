﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BuildFeed.Models
{
   public partial class Build
   {
      private const string BUILD_COLLECTION_NAME = "builds";
      private static readonly BsonDocument sortByDate = new BsonDocument(nameof(BuildModel.BuildTime), -1);

      private static readonly BsonDocument sortByOrder = new BsonDocument
                                                         {
                                                            new BsonElement(nameof(BuildModel.MajorVersion), -1),
                                                            new BsonElement(nameof(BuildModel.MinorVersion), -1),
                                                            new BsonElement(nameof(BuildModel.Number), -1),
                                                            new BsonElement(nameof(BuildModel.Revision), -1),
                                                            new BsonElement(nameof(BuildModel.BuildTime), -1)
                                                         };

      private readonly IMongoCollection<BuildModel> _buildCollection;
      private readonly IMongoDatabase _buildDatabase;
      private readonly MongoClient _dbClient;

      public Build()
      {
         _dbClient = new MongoClient
            (new MongoClientSettings
             {
                Server = new MongoServerAddress(MongoConfig.Host, MongoConfig.Port)
             });

         _buildDatabase = _dbClient.GetDatabase(MongoConfig.Database);
         _buildCollection = _buildDatabase.GetCollection<BuildModel>(BUILD_COLLECTION_NAME);
      }

      public async Task SetupIndexes()
      {
         var indexes = await (await _buildCollection.Indexes.ListAsync()).ToListAsync();
         if (indexes.All(i => i["name"] != "_idx_group"))
         {
            await _buildCollection.Indexes.CreateOneAsync(Builders<BuildModel>.IndexKeys.Combine(Builders<BuildModel>.IndexKeys.Descending(b => b.MajorVersion),
                                                                                                 Builders<BuildModel>.IndexKeys.Descending(b => b.MinorVersion),
                                                                                                 Builders<BuildModel>.IndexKeys.Descending(b => b.Number),
                                                                                                 Builders<BuildModel>.IndexKeys.Descending(b => b.Revision)), new CreateIndexOptions
                                                                                                                                                              {
                                                                                                                                                                 Name = "_idx_group"
                                                                                                                                                              });
         }

         if (indexes.All(i => i["name"] != "_idx_legacy"))
         {
            await _buildCollection.Indexes.CreateOneAsync(Builders<BuildModel>.IndexKeys.Ascending(b => b.LegacyId), new CreateIndexOptions
                                                                                                                     {
                                                                                                                        Name = "_idx_legacy"
                                                                                                                     });
         }
      }

      [DataObjectMethod(DataObjectMethodType.Select, true)]
      public async Task<List<BuildModel>> Select()
      {
         return await _buildCollection.Find(new BsonDocument())
                                      .ToListAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<BuildModel> SelectById(Guid id)
      {
         return await _buildCollection
                         .Find(Builders<BuildModel>.Filter.Eq(b => b.Id, id))
                         .SingleOrDefaultAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Select, false)]
      public async Task<BuildModel> SelectByLegacyId(long id)
      {
         return await _buildCollection
                         .Find(Builders<BuildModel>.Filter.Eq(b => b.LegacyId, id))
                         .SingleOrDefaultAsync();
      }

      [DataObjectMethod(DataObjectMethodType.Insert, true)]
      public async Task Insert(BuildModel item)
      {
         item.Id = Guid.NewGuid();
         item.LabUrl = item.GenerateLabUrl();
         await _buildCollection
            .InsertOneAsync(item);
      }

      [DataObjectMethod(DataObjectMethodType.Insert, false)]
      public async Task InsertAll(IEnumerable<BuildModel> items)
      {
         foreach (BuildModel item in items)
         {
            item.Id = Guid.NewGuid();
            item.LabUrl = item.GenerateLabUrl();
         }

         await _buildCollection
            .InsertManyAsync(items);
      }

      [DataObjectMethod(DataObjectMethodType.Update, true)]
      public async Task Update(BuildModel item)
      {
         BuildModel old = await SelectById(item.Id);
         item.Added = old.Added;
         item.Modified = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
         item.LabUrl = item.GenerateLabUrl();

         await _buildCollection
            .ReplaceOneAsync(Builders<BuildModel>.Filter.Eq(b => b.Id, item.Id), item);
      }

      [DataObjectMethod(DataObjectMethodType.Delete, true)]
      public async Task DeleteById(Guid id)
      {
         await _buildCollection
            .DeleteOneAsync(Builders<BuildModel>.Filter.Eq(b => b.Id, id));
      }
   }
}