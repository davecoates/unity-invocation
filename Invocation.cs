using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;


namespace Invocation {


    public static class Extensions
    {

        public static System.Reflection.MethodInfo GetBehaviourMethodByName(this GameObject obj, string name, out MonoBehaviour comp)
        {
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var component in components) {
                var type = component.GetType();
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var method in methods) {
                    if (method.Name == name) {
                        comp = component;
                        return method;
                    }
                }
            }
            comp = null;
            return null;
        }

        public static System.Reflection.MethodInfo GetBehaviourMethodByName(this GameObject obj, string name)
        {
            MonoBehaviour temp;
            return obj.GetBehaviourMethodByName(name,out temp);
        }

    }


    static class ArgIterator<T>
    {
        static int index= 0;

        static public T next(T[] args) {
            return args[index++];
        }
    }



    [System.Serializable]
    public class Command
    {

        public GameObject target;
        public string method;

        // Arguments that refer to Unity objects
        public UnityEngine.Object objref_arg_1;
        public UnityEngine.Object objref_arg_2;
        public UnityEngine.Object objref_arg_3;

        // All other arguments (strings, ints, floats, bools)
        public string value_arg_1;
        public string value_arg_2;
        public string value_arg_3;

        public string[] stringArgs;
        public Color[] colorArgs;
        public System.Int32[] intArgs;
        public Vector2[] vector2Args;

        public Color[] test_arg_1;

        public int int_arg_1;

        public Command() {}

        public void Invoke()
        {
            MonoBehaviour component;
            var methodInfo = target.GetBehaviourMethodByName(method, out component);
            if (methodInfo != null) {
                var parameters = methodInfo.GetParameters();
                var thisType = this.GetType();
                int objfield_count = 1,
                    valuefield_count = 1,
                    index = 0;
                string field_name;
                object[] args = new object[parameters.Length];

                foreach (var param in parameters) {
                    var paramType = param.ParameterType;
                    object value;
                    if (paramType.IsSubclassOf(typeof(UnityEngine.Object))) {
                        field_name = "objref_arg_"+objfield_count++;
                        value = thisType.GetField(field_name).GetValue(this);
                    } else {
                        field_name = "value_arg_"+valuefield_count++;
                        if (paramType == typeof(string)) {
                            value = ArgIterator<string>.next(stringArgs);
                        } else if (paramType == typeof(Color)) {
                            value = ArgIterator<Color>.next(colorArgs);
                        } else if (paramType == typeof(System.Int32)) {
                            value = ArgIterator<int>.next(intArgs);
                        } else if (paramType == typeof(Vector2)) {
                            value = ArgIterator<Vector2>.next(vector2Args);
                        } else {
                            value = null;
                        }

                    }
                    args[index++] = System.Convert.ChangeType(value, param.ParameterType);
                }
                methodInfo.Invoke(component, args);
            }
        }

    }

}
