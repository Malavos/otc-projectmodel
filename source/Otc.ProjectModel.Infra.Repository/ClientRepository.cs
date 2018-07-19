﻿using Dapper;
using Otc.ProjectModel.Core.Domain.Models;
using Otc.ProjectModel.Core.Domain.Repositories;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Otc.ProjectModel.Infra.Repository
{
    public class ClientRepository : IClientReadOnlyRepository, IClientWriteOnlyRepository
    {
        private readonly IDbConnection _dbConnection;

        public ClientRepository(IDbConnection dbConnection)
        {
            this._dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        }

        public async Task AddClientAsync(Client client)
        {
            var clientParams = new DynamicParameters();
            clientParams.Add("Id", client.Id, DbType.Guid);
            clientParams.Add("Nome", client.Name, DbType.AnsiString);
            clientParams.Add("Email", client.Email, DbType.AnsiString);

            var addressParams = new DynamicParameters();
            addressParams.Add("Address.ClientId", client.Id, DbType.Guid);
            addressParams.Add("Address.Street", client.Address.Street, DbType.AnsiString);
            addressParams.Add("Address.Number", client.Address.Number, DbType.AnsiString);
            addressParams.Add("Address.Neighborhood", client.Address.Neighborhood, DbType.AnsiString);
            addressParams.Add("Address.City", client.Address.City, DbType.AnsiString);
            addressParams.Add("Address.State", client.Address.State, DbType.AnsiString);
            addressParams.Add("Address.Country", client.Address.Country, DbType.AnsiString);
            addressParams.Add("Address.ZipCode", client.Address.ZipCode, DbType.AnsiString);

            var queryClient = @"INSERT INTO Client (Id, Name, Email) VALUES (@Id, @Name, @Email)";
            var queryClientAddress = @"INSERT INTO Address (ClientId, Street, Number, Neighborhood, City, State, Country, ZipCode) VALUES (@ClientId, @Street, @Number, @Neighborhood, @City, @State, @Country, @ZipCode)";

            using (var trans = new TransactionScope())
            {
                if (await _dbConnection.ExecuteAsync(queryClient, clientParams) > 0)
                    if (await _dbConnection.ExecuteAsync(queryClientAddress, addressParams) > 0)
                        trans.Complete();
            }
        }

        public async Task<Client> GetClientAsync(Guid clientId)
        {
            var clientParams = new DynamicParameters();
            clientParams.Add("Id", clientId, DbType.Guid);

            var query = @"SELECT Id, Name, Email FROM Client Where Id = @Id";

            var client = await _dbConnection.QueryAsync<Client>(query, clientParams);

            return client.SingleOrDefault();
        }

        public async Task RemoveClientAsync(Guid clientId)
        {
            var clientParams = new DynamicParameters();
            clientParams.Add("Id", clientId, DbType.Guid);

            var deleteAddress = @"DELETE FROM Address WHERE ClientId = @ClientId";
            var deletePayments = @"DELETE FROM Payments WHERE SubscriptionId = @SubscriptionId";
            var deleteSubscriptions = @"DELETE FROM Subscriptions WHERE ClientId = @ClientId";
            var deleteClient = @"DELETE FROM Clients WHERE ClientId = @ClientId";

            var selectSubscritionsId = @"SELECT SubscritionId WHERE ClientId = @ClientID";

            using (var trans = _dbConnection.BeginTransaction())
            {

                var subscritionsId = await _dbConnection.QueryAsync<Guid>(selectSubscritionsId);

                foreach (var subId in subscritionsId.ToList())
                    await _dbConnection.ExecuteAsync(deletePayments, new { Number = subId });

                await _dbConnection.ExecuteAsync(deleteSubscriptions, clientParams);
                await _dbConnection.ExecuteAsync(deleteAddress, clientParams);
                await _dbConnection.ExecuteAsync(deleteClient, clientParams);

                trans.Commit();
            }
        }

        public async Task UpdateClientAsync(Client client)
        {
            var clientParams = new DynamicParameters();
            clientParams.Add("Id", client.Id, DbType.Guid);
            clientParams.Add("Nome", client.Name, DbType.AnsiString);
            clientParams.Add("Email", client.Email, DbType.AnsiString);

            var addressParams = new DynamicParameters();
            addressParams.Add("Address.ClientId", client.Id, DbType.Guid);
            addressParams.Add("Address.Street", client.Address.Street, DbType.AnsiString);
            addressParams.Add("Address.Number", client.Address.Number, DbType.AnsiString);
            addressParams.Add("Address.Neighborhood", client.Address.Neighborhood, DbType.AnsiString);
            addressParams.Add("Address.City", client.Address.City, DbType.AnsiString);
            addressParams.Add("Address.State", client.Address.State, DbType.AnsiString);
            addressParams.Add("Address.Country", client.Address.Country, DbType.AnsiString);
            addressParams.Add("Address.ZipCode", client.Address.ZipCode, DbType.AnsiString);

            var queryClient = @"UPDATE Client SET Name = @Name, Email = @Email) VALUES (@Name, @Email) WHERE ClientId = @ClientId";
            var queryClientAddress = @"UPDATE Address SET Street = @Street, Number = @Number, Neighborhood = @Neighborhood, City = @City, State = @State, Country = @Country, ZipCode = @ZipCode WHERE ClientId = @ClientId";

            using (var trans = new TransactionScope())
            {
                if (await _dbConnection.ExecuteAsync(queryClient, clientParams) > 0)
                    if (await _dbConnection.ExecuteAsync(queryClientAddress, addressParams) > 0)
                        trans.Complete();
            }
        }
    }
}
