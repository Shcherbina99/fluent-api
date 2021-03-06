using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner> : IPrintingConfig
    {
        private readonly HashSet<Type> excludingTypes;
        private readonly HashSet<MemberInfo> excludingProperty;
        private readonly Dictionary<Type, Func<object, string>> typeSerialisation;
        private readonly Dictionary<MemberInfo, Func<object, string>> propertySerialisation;
        private readonly Stack<object> nestingStack;

        Dictionary<Type, Func<object, string>> IPrintingConfig.typeSerialisation => typeSerialisation;
        Dictionary<MemberInfo, Func<object, string>> IPrintingConfig.propertySerialisation => propertySerialisation;

        private static readonly Type[] FinalTypes =
        {
            typeof(int), typeof(double), typeof(float), typeof(string),
            typeof(DateTime), typeof(TimeSpan)
        };

        public PrintingConfig()
        {
            excludingTypes = new HashSet<Type>();
            excludingProperty = new HashSet<MemberInfo>();
            typeSerialisation = new Dictionary<Type, Func<object, string>>();
            propertySerialisation = new Dictionary<MemberInfo, Func<object, string>>();
            nestingStack = new Stack<object>();
        }

        public string PrintToString(TOwner obj)
        {
            return PrintToString(obj, 0);
        }

        private string PrintToString(object obj, int nestingLevel)
        {
            if (obj == null)
                return "null" + Environment.NewLine;

            if (FinalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;

            if (nestingStack.Contains(obj))
            {
                return $"circle reference";
            }
            nestingStack.Push(obj);

            if (obj is IEnumerable enumerable)
            {
                return PrintCollection(enumerable, nestingLevel);
            }
            
            var result = PrintClass(obj, nestingLevel);

            if(nestingStack.Count != 0)
                nestingStack.Pop();
            return result;
        }

        private string PrintObjWithMembers<T>(string nameObj, IEnumerable<T> serMembers, Func<T, string> serFunc, int nestingLevel)
        {
            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            sb.AppendLine(nameObj);
            foreach (var serObj in serMembers)
            {
                sb.Append(identation);
                sb.Append(serFunc(serObj));
            }

            return sb.ToString();
        }

        private string PrintCollection(IEnumerable enumerable, int nestingLevel)
        {
            return PrintObjWithMembers(enumerable.GetType().Name, enumerable.Cast<object>(), (el) => PrintToString(el, nestingLevel + 1),
                nestingLevel);
        }

        private string PrintClass(object obj, int nestingLevel)
        {
            var type = obj.GetType();
            var approvedProp = type.GetProperties().Where(prop =>
                !excludingTypes.Contains(prop.PropertyType) && !excludingProperty.Contains(prop));
            var approvedField = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !excludingTypes.Contains(field.FieldType) && !excludingProperty.Contains(field));

            var serMembers = approvedProp.Cast<MemberInfo>().Concat(approvedField);

            return PrintObjWithMembers(type.Name, serMembers,
                info =>
                {
                    switch (info)
                    {
                        case PropertyInfo propertyInfo:
                            return $"{propertyInfo.Name} = {PrintProperty(propertyInfo, obj, nestingLevel + 1)}";
                        case FieldInfo fieldInfo:
                            return $"{fieldInfo.Name} = {PrintField(fieldInfo, obj, nestingLevel + 1)}";
                        default:
                            throw new ArgumentException();
                    }
                },
                nestingLevel);
        }

        private string PrintProperty(PropertyInfo propertyInfo, object obj, int nestingLevel)
        {
            var value = propertyInfo.GetValue(obj);
            
            if (propertySerialisation.ContainsKey(propertyInfo))
            {
                return PrintToString(propertySerialisation[propertyInfo].DynamicInvoke(value), nestingLevel);
            }

            if (typeSerialisation.ContainsKey(propertyInfo.PropertyType))
            {
                return PrintToString(typeSerialisation[propertyInfo.PropertyType].DynamicInvoke(value), nestingLevel);
            }

            return PrintToString(value, nestingLevel);
        }

        private object PrintField(FieldInfo fieldInfo, object obj, int nestingLevel)
        {
            var value = fieldInfo.GetValue(obj);
            
            if (propertySerialisation.ContainsKey(fieldInfo))
            {
                return PrintToString(propertySerialisation[fieldInfo].DynamicInvoke(value), nestingLevel);
            }

            if (typeSerialisation.ContainsKey(fieldInfo.FieldType))
            {
                return PrintToString(typeSerialisation[fieldInfo.FieldType].DynamicInvoke(value), nestingLevel);
            }

            return PrintToString(value, nestingLevel);
        }

        public PropertySerializingConfig<TOwner, T> AlternativeFor<T>()
        {
            return new PropertySerializingConfig<TOwner, T>(this);
        }

        public PropertySerializingConfig<TOwner, T> AlternativeFor<T>(Expression<Func<TOwner, T>> func)
        {
            if (func.Body is MemberExpression memberExpression && IsPropertyOrField(memberExpression.Member))
            {
                return new PropertySerializingConfig<TOwner, T>(this, memberExpression.Member);
            }

            throw new ArgumentException();
        }

        public PrintingConfig<TOwner> Excluding<T>()
        {
            excludingTypes.Add(typeof(T));
            return this;
        }

        public PrintingConfig<TOwner> Excluding<TPropType>(Expression<Func<TOwner, TPropType>> func)
        {
            if (func.Body is MemberExpression memberExpression && IsPropertyOrField(memberExpression.Member))
            {
                excludingProperty.Add(memberExpression.Member);
            }
            else
            {
                throw new ArgumentException();
            }

            return this;
        }

        private static bool IsPropertyOrField(MemberInfo memberInfo)
        {
            return memberInfo.MemberType == MemberTypes.Property || memberInfo.MemberType == MemberTypes.Field;
        }
    }
}