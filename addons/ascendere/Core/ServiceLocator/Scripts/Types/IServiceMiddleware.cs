using System;
using System.Threading.Tasks;

public interface IServiceMiddleware
{
    Task<object> InvokeAsync(ServiceContext context, Func<Task<object>> next);
}
