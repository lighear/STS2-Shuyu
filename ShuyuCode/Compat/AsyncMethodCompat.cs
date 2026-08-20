using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shuyu.Compat;

public static class AsyncMethodCompat
{
    public static Type? GetStateMachineType(MethodBase method)
    {
        Type? declaringType = method.DeclaringType;
        if (method.Name == nameof(IAsyncStateMachine.MoveNext)
            && declaringType != null
            && typeof(IAsyncStateMachine).IsAssignableFrom(declaringType))
        {
            return declaringType;
        }

        return method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
    }
}
