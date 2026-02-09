using System;
using System.Collections.Generic;
using Godot;

namespace Ascendere.Utils;

public static class VariantUtils
{
    public static Variant ToVariant(object value)
    {
        if (value == null)
            return new Variant();

        return value switch
        {
            bool b => (Variant)b,
            int i => (Variant)i,
            float f => (Variant)f,
            double d => (Variant)(float)d,
            string s => (Variant)s,
            Vector2 v2 => (Variant)v2,
            Vector3 v3 => (Variant)v3,
            Color c => (Variant)c,
            Node node => (Variant)node,
            Resource resource => (Variant)resource,
            //Array array => Variant.CreateFrom(array),
            // Dictionary dict => (Variant)dict,
            _ => throw new ArgumentException(
                $"Unsupported type for Variant conversion: {value.GetType()}"
            ),
        };
    }

    public static T FromVariant<T>(Variant variant)
    {
        if (variant.Obj == null)
            return default;

        Type targetType = typeof(T);
        object result = targetType switch
        {
            Type t when t == typeof(bool) => variant.AsBool(),
            Type t when t == typeof(int) => variant.AsInt32(),
            Type t when t == typeof(float) => variant.AsSingle(),
            Type t when t == typeof(double) => variant.AsSingle(),
            Type t when t == typeof(string) => variant.AsString(),
            Type t when t == typeof(Vector2) => variant.AsVector2(),
            Type t when t == typeof(Vector3) => variant.AsVector3(),
            Type t when t == typeof(Color) => variant.AsColor(),
            Type t when t.IsAssignableTo(typeof(Node)) => variant.As<Node>(),
            Type t when t.IsAssignableTo(typeof(Resource)) => variant.As<Resource>(),
            _ => throw new ArgumentException(
                $"Unsupported type for Variant conversion: {targetType}"
            ),
        };

        return (T)result;
    }
}
