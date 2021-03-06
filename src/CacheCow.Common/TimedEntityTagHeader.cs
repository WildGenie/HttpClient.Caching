﻿namespace CacheCow.Common
{
    using System;
    using System.Net.Http.Headers;

    public class TimedEntityTagHeaderValue : EntityTagHeaderValue
    {
        public TimedEntityTagHeaderValue(string tag)
            : this(tag, false)
        {}

        public TimedEntityTagHeaderValue(EntityTagHeaderValue entityTagHeaderValue)
            : this(entityTagHeaderValue.Tag, entityTagHeaderValue.IsWeak)
        {}

        public TimedEntityTagHeaderValue(string tag, bool isWeak)
            : base(tag, isWeak)
        {
            LastModified = DateTimeOffset.Parse(DateTimeOffset.UtcNow.ToString("r")); // to remove milliseconds
        }

        public DateTimeOffset LastModified { get; set; }


        public override string ToString()
        {
            return base.ToString() + "\r\n" + LastModified.ToString("r");
        }

        public static bool TryParse(string timedETagValue, out TimedEntityTagHeaderValue value)
        {
            value = null;
            if(timedETagValue == null)
                return false;

            var strings = timedETagValue.Split(new[] {"\r\n"}, StringSplitOptions.None);
            if(strings.Length != 2)
                return false;

            EntityTagHeaderValue etag = null;
            DateTimeOffset lastModified;
            if(!TryParse(strings[0], out etag))
                return false;

            if(!DateTimeOffset.TryParse(strings[1], out lastModified))
                return false;

            value = new TimedEntityTagHeaderValue(etag.Tag, etag.IsWeak)
            {
                LastModified = lastModified
            };
            return true;
        }

        public EntityTagHeaderValue ToEntityTagHeaderValue()
        {
            return new EntityTagHeaderValue(Tag, IsWeak);
        }
    }
}