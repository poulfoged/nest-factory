using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestClientFactory
{
    public interface IClientConfigurator
    {
        void Configure(IClientFactory factory);

    }
}
