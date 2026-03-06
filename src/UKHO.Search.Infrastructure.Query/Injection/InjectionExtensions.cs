using Microsoft.Extensions.DependencyInjection;

namespace UKHO.Search.Infrastructure.Query.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddQueryServices(this IServiceCollection collection)
        {
            return collection;
        }
    }
}