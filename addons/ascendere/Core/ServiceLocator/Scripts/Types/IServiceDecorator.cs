public interface IServiceDecorator<T>
    where T : class
{
    T Decorate(T instance);
}
