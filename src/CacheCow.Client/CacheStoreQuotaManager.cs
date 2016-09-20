namespace CacheCow.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class CacheStoreQuotaManager
    {
        private bool _doingHousekeeping = false;
        private readonly object _lock = new object();
        private readonly ICacheMetadataProvider _metadataProvider;
        private readonly bool _needsGrandTotalHouseKeeping;
        private readonly bool _needsPerDomainHouseKeeping;
        private readonly Action<CacheItemMetadata> _remover;
        private readonly CacheStoreSettings _settings;
        internal long GrandTotal;
        internal ConcurrentDictionary<string, long> StorageMetadata = new ConcurrentDictionary<string, long>();

        /// <summary>
        /// </summary>
        /// <param name="metadataProvider">Most likely implemented by the cache store itself</param>
        /// <param name="remover">
        ///     This is a method most likely on the cache store which does not call
        ///     back on ItemRemoved. This is very important.
        /// </param>
        public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<CacheItemMetadata> remover)
            : this(metadataProvider, remover, new CacheStoreSettings())
        {}

        public CacheStoreQuotaManager(ICacheMetadataProvider metadataProvider, Action<CacheItemMetadata> remover,
            CacheStoreSettings settings)
        {
            _remover = remover;
            _metadataProvider = metadataProvider;
            _settings = settings;
            _needsGrandTotalHouseKeeping = settings.TotalQuota > 0;
            _needsPerDomainHouseKeeping = settings.PerDomainQuota > 0;
            BuildStorageMetadata();
        }

        public virtual void Clear()
        {
            StorageMetadata.Clear();
            GrandTotal = 0;
        }

        public virtual void ItemAdded(CacheItemMetadata metadata)
        {
            StorageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l + metadata.Size);
            var total = StorageMetadata[metadata.Domain];
            lock(_lock)
            {
                GrandTotal += metadata.Size;
            }

            if(_needsGrandTotalHouseKeeping && (GrandTotal > _settings.TotalQuota))
                DoHouseKeepingAsync();

            if(_needsPerDomainHouseKeeping && (total > _settings.PerDomainQuota))
                DoDomainHouseKeepingAsync(metadata.Domain);
        }

        public virtual void ItemRemoved(CacheItemMetadata metadata)
        {
            StorageMetadata.AddOrUpdate(metadata.Domain, metadata.Size, (d, l) => l - metadata.Size);
            lock(_lock)
            {
                GrandTotal -= metadata.Size;
            }
        }

        private void BuildStorageMetadata()
        {
            var domainSizes = _metadataProvider.GetDomainSizes();
            foreach(var domainSize in domainSizes)
            {
                lock(_lock)
                {
                    GrandTotal += domainSize.Value;
                }
                StorageMetadata.AddOrUpdate(domainSize.Key, domainSize.Value, (k, v) => domainSize.Value);
            }
        }

        private void DoHouseKeeping()
        {
            while(GrandTotal > _settings.TotalQuota)
            {
                var item = _metadataProvider.GetEarliestAccessedItem();
                if(item != null)
                {
                    _remover(item);
                    ItemRemoved(item);
                }
            }
        }

        private void DoHouseKeepingAsync()
        {
            Task.Factory.StartNew(DoHouseKeeping)
                .ContinueWith(t =>
                {
                    if(t.IsFaulted)
                        Trace.WriteLine(t.Exception);
                });
        }


        private void DoDomainHouseKeeping(object domain)
        {
            var dom = (string) domain;
            while(StorageMetadata[dom] > _settings.PerDomainQuota)
            {
                var item = _metadataProvider.GetEarliestAccessedItem(dom);
                if(item != null)
                {
                    _remover(item);
                    ItemRemoved(item);
                }
            }
        }


        private void DoDomainHouseKeepingAsync(string domain)
        {
            Task.Factory.StartNew(DoDomainHouseKeeping, domain)
                .ContinueWith(t =>
                {
                    if(t.IsFaulted)
                        Trace.WriteLine(t.Exception);
                });
        }
    }
}