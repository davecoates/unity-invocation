using UnityEngine;
using System.Collections;
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
        public object value_arg_1;
        public object value_arg_2;
        public object value_arg_3;

        public Command() {}

        public void Invoke()
        {
            if (target != null && !string.IsNullOrEmpty(method)) {
                Debug.Log(method);
            }
        }

    }

}
