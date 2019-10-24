﻿using System;
using System.Data;
using VDS.RDF.Storage;

namespace Semiodesk.Trinity.Store.Stardog
{
    /// <summary>
    /// Wrapper to support Stardog transactions.  At present, nested transaction is NOT supported as the transaction instance is a wrapper around the StardogConnector.
    /// </summary>
    public class StardogTransaction : ITransaction
    {
        /// <summary>
        /// StardogConnector instance associated with this transaction
        /// </summary>
        public StardogConnector Connector { get; }
        /// <inheritdoc cref="ITransaction"/>
        public StardogTransaction(StardogConnector connector)
        {
            Connector = connector;
#if !NET35
            Connector.Begin(true);
#else
            Connector.Begin();
#endif
            IsActive = true;
        }
        
        /// <summary>
        /// Instance is active and has not been committed or rolled back.
        /// Once the transaction has been committed or rolled back, it should be disposed of.
        /// </summary>
        public bool IsActive
        {
            get;
            private set;
        }
        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            Rollback();
        }      

        /// <inheritdoc cref="ITransaction"/>
        public event FinishedTransactionEvent OnFinishedTransaction;

        /// <inheritdoc cref="ITransaction"/>
        public void Commit()
        {
            // We'll let the underlying connector throw here which it will if Commit is called on a non-active transaction.
            Connector.Commit();
            OnFinishedTransaction?.Invoke(this, new TransactionEventArgs(true));
            IsActive = false;
        }

        /// <inheritdoc cref="ITransaction"/>
        public void Rollback()
        {
            if (!IsActive) return;
            Connector.Rollback();
            AddTripleCount = 0;
            RemoveTripleCount = 0;
            OnFinishedTransaction?.Invoke(this, new TransactionEventArgs(false));
            IsActive = false;
        }

        /// <inheritdoc cref="ITransaction"/>
        public IsolationLevel IsolationLevel => IsolationLevel.Snapshot;

        /// <summary>
        /// Number of pending "Additions" in the current transaction.
        /// </summary>
        public int AddTripleCount { get; internal set; }
        /// <summary>
        /// Number of pending "Removals" in the current transaction.
        /// </summary>
        public int RemoveTripleCount { get; internal set; }
        /// <summary>
        /// Has any pending changes.
        /// </summary>
        public bool HasPendingChanges => (AddTripleCount + AddTripleCount) > 0;
    }
}
