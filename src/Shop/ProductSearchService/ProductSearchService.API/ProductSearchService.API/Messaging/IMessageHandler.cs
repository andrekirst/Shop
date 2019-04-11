﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductSearchService.API.Messaging
{
    public interface IMessageHandler
    {
        void Start(IMessageHandlerCallback callback);

        void Stop();
    }
}
