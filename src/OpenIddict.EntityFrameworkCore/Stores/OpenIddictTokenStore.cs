﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Core;
using OpenIddict.Models;

namespace OpenIddict.EntityFrameworkCore
{
    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    public class OpenIddictTokenStore<TContext> : OpenIddictTokenStore<OpenIddictToken,
                                                                       OpenIddictApplication,
                                                                       OpenIddictAuthorization, TContext, string>
        where TContext : DbContext
    {
        public OpenIddictTokenStore([NotNull] TContext context) : base(context) { }
    }

    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    /// <typeparam name="TKey">The type of the entity primary keys.</typeparam>
    public class OpenIddictTokenStore<TContext, TKey> : OpenIddictTokenStore<OpenIddictToken<TKey>,
                                                                             OpenIddictApplication<TKey>,
                                                                             OpenIddictAuthorization<TKey>, TContext, TKey>
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        public OpenIddictTokenStore([NotNull] TContext context) : base(context) { }
    }

    /// <summary>
    /// Provides methods allowing to manage the tokens stored in a database.
    /// Note: this class can only be used with the default OpenIddict entities.
    /// </summary>
    /// <typeparam name="TToken">The type of the Token entity.</typeparam>
    /// <typeparam name="TApplication">The type of the Application entity.</typeparam>
    /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
    /// <typeparam name="TContext">The type of the Entity Framework database context.</typeparam>
    /// <typeparam name="TKey">The type of the entity primary keys.</typeparam>
    public class OpenIddictTokenStore<TToken, TApplication, TAuthorization, TContext, TKey> :
        OpenIddictTokenStore<TToken, TApplication, TAuthorization, TKey>
        where TToken : OpenIddictToken<TKey, TApplication, TAuthorization>, new()
        where TApplication : OpenIddictApplication<TKey, TAuthorization, TToken>, new()
        where TAuthorization : OpenIddictAuthorization<TKey, TApplication, TToken>, new()
        where TContext : DbContext
        where TKey : IEquatable<TKey>
    {
        public OpenIddictTokenStore([NotNull] TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Context = context;
        }

        /// <summary>
        /// Gets the database context associated with the current store.
        /// </summary>
        protected virtual TContext Context { get; }

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TApplication"/> entity.
        /// </summary>
        protected DbSet<TApplication> Applications => Context.Set<TApplication>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TAuthorization"/> entity.
        /// </summary>
        protected DbSet<TAuthorization> Authorizations => Context.Set<TAuthorization>();

        /// <summary>
        /// Gets the database set corresponding to the <typeparamref name="TToken"/> entity.
        /// </summary>
        protected DbSet<TToken> Tokens => Context.Set<TToken>();

        /// <summary>
        /// Determines the number of tokens that match the specified query.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the number of tokens that match the specified query.
        /// </returns>
        public override Task<long> CountAsync<TResult>([NotNull] Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query(Tokens).LongCountAsync();
        }

        /// <summary>
        /// Creates a new token.
        /// </summary>
        /// <param name="token">The token to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result returns the token.
        /// </returns>
        public override async Task<TToken> CreateAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Add(token);

            await Context.SaveChangesAsync(cancellationToken);

            return token;
        }

        /// <summary>
        /// Creates a new token.
        /// </summary>
        /// <param name="descriptor">The token descriptor.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation, whose result returns the token.
        /// </returns>
        public override async Task<TToken> CreateAsync([NotNull] OpenIddictTokenDescriptor descriptor, CancellationToken cancellationToken)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var token = new TToken();

            await BindAsync(token, descriptor, cancellationToken);
            return await CreateAsync(token, cancellationToken);
        }

        /// <summary>
        /// Removes a token.
        /// </summary>
        /// <param name="token">The token to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>A <see cref="Task"/> that can be used to monitor the asynchronous operation.</returns>
        public override Task DeleteAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Remove(token);

            return Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves the list of tokens corresponding to the specified application identifier.
        /// </summary>
        /// <param name="identifier">The application identifier associated with the tokens.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the tokens corresponding to the specified application.
        /// </returns>
        public override async Task<ImmutableArray<TToken>> FindByApplicationIdAsync([NotNull] string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            // Note: due to a bug in Entity Framework Core's query visitor, the tokens can't be
            // filtered using token.Application.Id.Equals(key). To work around this issue,
            // this method is overriden to use an explicit join before applying the equality check.
            // See https://github.com/openiddict/openiddict-core/issues/499 for more information.

            IQueryable<TToken> Query(IQueryable<TApplication> applications, IQueryable<TToken> tokens)
            {
                var key = ConvertIdentifierFromString(identifier);

                return from token in tokens
                       join application in applications on token.Application.Id equals application.Id
                       where application.Id.Equals(key)
                       select token;
            }

            return ImmutableArray.CreateRange(await Query(Applications, Tokens).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Retrieves the list of tokens corresponding to the specified authorization identifier.
        /// </summary>
        /// <param name="identifier">The authorization identifier associated with the tokens.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the tokens corresponding to the specified authorization.
        /// </returns>
        public override async Task<ImmutableArray<TToken>> FindByAuthorizationIdAsync([NotNull] string identifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("The identifier cannot be null or empty.", nameof(identifier));
            }

            // Note: due to a bug in Entity Framework Core's query visitor, the tokens can't be
            // filtered using token.Authorization.Id.Equals(key). To work around this issue,
            // this method is overriden to use an explicit join before applying the equality check.
            // See https://github.com/openiddict/openiddict-core/issues/499 for more information.

            IQueryable<TToken> Query(IQueryable<TAuthorization> authorizations, IQueryable<TToken> tokens)
            {
                var key = ConvertIdentifierFromString(identifier);

                return from token in tokens
                       join authorization in authorizations on token.Authorization.Id equals authorization.Id
                       where authorization.Id.Equals(key)
                       select token;
            }

            return ImmutableArray.CreateRange(await Query(Authorizations, Tokens).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Executes the specified query and returns the first element.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns the first element returned when executing the query.
        /// </returns>
        public override Task<TResult> GetAsync<TResult>([NotNull] Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query(Tokens).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the specified query and returns all the corresponding elements.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation,
        /// whose result returns all the elements returned when executing the specified query.
        /// </returns>
        public override async Task<ImmutableArray<TResult>> ListAsync<TResult>([NotNull] Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return ImmutableArray.CreateRange(await query(Tokens).ToListAsync(cancellationToken));
        }

        /// <summary>
        /// Sets the application identifier associated with a token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="identifier">The unique identifier associated with the client application.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override async Task SetApplicationIdAsync([NotNull] TToken token, [CanBeNull] string identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var key = ConvertIdentifierFromString(identifier);

                var application = await Applications.SingleOrDefaultAsync(element => element.Id.Equals(key), cancellationToken);
                if (application == null)
                {
                    throw new InvalidOperationException("The application associated with the token cannot be found.");
                }

                token.Application = application;
            }

            else
            {
                var key = await GetIdAsync(token, cancellationToken);

                // Try to retrieve the application associated with the token.
                // If none can be found, assume that no application is attached.
                var application = await Applications.FirstOrDefaultAsync(element => element.Tokens.Any(t => t.Id.Equals(key)));
                if (application != null)
                {
                    application.Tokens.Remove(token);
                }
            }
        }

        /// <summary>
        /// Sets the authorization identifier associated with a token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="identifier">The unique identifier associated with the authorization.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override async Task SetAuthorizationIdAsync([NotNull] TToken token, [CanBeNull] string identifier, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!string.IsNullOrEmpty(identifier))
            {
                var key = ConvertIdentifierFromString(identifier);

                var authorization = await Authorizations.SingleOrDefaultAsync(element => element.Id.Equals(key), cancellationToken);
                if (authorization == null)
                {
                    throw new InvalidOperationException("The authorization associated with the token cannot be found.");
                }

                token.Authorization = authorization;
            }

            else
            {
                var key = await GetIdAsync(token, cancellationToken);

                // Try to retrieve the authorization associated with the token.
                // If none can be found, assume that no authorization is attached.
                var authorization = await Authorizations.FirstOrDefaultAsync(element => element.Tokens.Any(t => t.Id.Equals(key)));
                if (authorization != null)
                {
                    authorization.Tokens.Remove(token);
                }
            }
        }

        /// <summary>
        /// Updates an existing token.
        /// </summary>
        /// <param name="token">The token to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        public override Task UpdateAsync([NotNull] TToken token, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            Context.Attach(token);

            // Generate a new concurrency token and attach it
            // to the token before persisting the changes.
            token.ConcurrencyToken = Guid.NewGuid().ToString();

            Context.Update(token);

            return Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Sets the token properties based on the specified descriptor.
        /// </summary>
        /// <param name="token">The token to update.</param>
        /// <param name="descriptor">The token descriptor.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to abort the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that can be used to monitor the asynchronous operation.
        /// </returns>
        protected virtual async Task BindAsync([NotNull] TToken token, [NotNull] OpenIddictTokenDescriptor descriptor, CancellationToken cancellationToken)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            token.Ciphertext = descriptor.Ciphertext;
            token.CreationDate = descriptor.CreationDate;
            token.ExpirationDate = descriptor.ExpirationDate;
            token.Hash = descriptor.Hash;
            token.Status = descriptor.Status;
            token.Subject = descriptor.Subject;
            token.Type = descriptor.Type;

            // Bind the token to the specified client application, if applicable.
            if (!string.IsNullOrEmpty(descriptor.ApplicationId))
            {
                var key = ConvertIdentifierFromString(descriptor.ApplicationId);

                var application = await Applications.SingleOrDefaultAsync(entity => entity.Id.Equals(key));
                if (application == null)
                {
                    throw new InvalidOperationException("The application associated with the token cannot be found.");
                }

                token.Application = application;
            }

            // Bind the token to the specified authorization, if applicable.
            if (!string.IsNullOrEmpty(descriptor.AuthorizationId))
            {
                var key = ConvertIdentifierFromString(descriptor.AuthorizationId);

                var authorization = await Authorizations.SingleOrDefaultAsync(entity => entity.Id.Equals(key));
                if (authorization == null)
                {
                    throw new InvalidOperationException("The authorization associated with the token cannot be found.");
                }

                token.Authorization = authorization;
            }
        }
    }
}