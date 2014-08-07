using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Invocation {

    [CustomPropertyDrawer(typeof(Command))]
    public class CommandPropertyDrawer : PropertyDrawer
    {

        /**
         * Helper function to track instance a GUI field (f), using the
         * specified value property (eg. stringValue, intValue etc). Internally
         * the values are stored in an array of type T so we return a lambda
         * that tracks the current index we are up to each time it's called.
         * 
         * @see Command for list of argument properties (stringArgs, intArgs
         * etc)
         *
         * @param SerializedProperty args this is the field for the current
         * arguments array we are considering (eg. stringArgs)
         * @param string propertyName the property that contains the value (eg.
         * stringValue, intValue, colorValue etc)
         * @param Func<Rect, T, T> f a function that creates a field, eg
         * EditorGUI.TextField 
         *
         */
        static Func<Rect, string, SerializedProperty> ArgEditField<T>(SerializedProperty args, string propertyName, Func<Rect, string, T, T> f)
        {
            int index = 0;
            return (Rect pos, string label) => {
                if (index <= args.arraySize) {
                    args.InsertArrayElementAtIndex(index);
                }
                var arg = args.GetArrayElementAtIndex(index);
                index++;

                EditorGUI.BeginChangeCheck ();
                var field = arg.GetType().GetProperty(propertyName);
                if (null == field) {
                    throw new NullReferenceException("Field "+propertyName+" not found on property");
                }
                T value = f(pos, label, (T)field.GetValue(arg, null));
                if (EditorGUI.EndChangeCheck ()){
                    field.SetValue(arg, value, null);
                }

                return arg;
            };
        }


        private GameObject getTarget(SerializedProperty property) 
        {
            var targetProperty = property.FindPropertyRelative("target");
            var obj = targetProperty.objectReferenceValue as GameObject;
            if (obj == null) {
                // Default to parent gameobject if available
                var parent = property.serializedObject.targetObject as MonoBehaviour;
                if (parent) obj = parent.gameObject;
            }

            return obj;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            GameObject target = getTarget(property);

            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            label.text += " (Command)";
            EditorGUI.LabelField(position, label, EditorStyles.label);

            EditorGUI.indentLevel = indent;

            SerializedProperty targetProp = property.FindPropertyRelative("target"),
                               methodProp = property.FindPropertyRelative("method");

            // We need to know the selected method (if any) so we can allocate
            // space in the UI for it's parameters
            var selectedMethod = target.GetBehaviourMethodByName(methodProp.stringValue);
            int argCount = 0;
            if (selectedMethod != null) {
                argCount = selectedMethod.GetParameters().Length;
            }

            int lineNumber = 1,
                index = 0, // Index into method names list
                controlCount = 3 + argCount;

            // Helper function to make a label and return Rect for the
            // corresponding field. This tracks the current 'line' we are on so
            // the next call to makeLabel is moved down a line on the UI
            Func<string, Rect> makeLabel = (string l) => {
                var p = new Rect(position.x, position.y + position.height/controlCount * lineNumber++, position.width, position.height / controlCount) ;
                return EditorGUI.PrefixLabel(p, new GUIContent(l));
            };

            targetProp.objectReferenceValue = EditorGUI.ObjectField(
                    makeLabel("Target"), target, typeof(GameObject), true);

            var components = target.GetComponents<MonoBehaviour>();
            var methodNames = new List<string>();
            var names = new List<string>();
            names.Add("(Select method)");
            methodNames.Add(null);
            foreach (var component in components) {
                // TODO: Blacklist some methods? Use attribute to flag methods
                // manually?
                var type = component.GetType();
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var method in methods) {
                    if (method.Name == methodProp.stringValue) {
                        index = methodNames.Count;
                    }

                    methodNames.Add(method.Name);
                    names.Add(type.Name + ": " + method.Name);
                }
            }


            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(makeLabel("Method"), index, names.ToArray());
            if (EditorGUI.EndChangeCheck()) {
                methodProp.stringValue = methodNames[index];
            }

            if (selectedMethod != null) {
                // Now we add edit fields for all parameters of selected method
                var parameters = selectedMethod.GetParameters();
                int objRefParamCount = 0, otherParamCount = 0;
                SerializedProperty stringArgs = property.FindPropertyRelative("stringArgs"),
                                   intArgs = property.FindPropertyRelative("intArgs"),
                                   colorArgs = property.FindPropertyRelative("colorArgs"),
                                   vector2Args = property.FindPropertyRelative("vector2Args");
                var stringArgField = ArgEditField<string>(stringArgs, "stringValue", EditorGUI.TextField);
                var colorArgField = ArgEditField<Color>(colorArgs, "colorValue", EditorGUI.ColorField);
                var intArgField = ArgEditField<int>(intArgs, "intValue", EditorGUI.IntField);
                var vector2ArgField = ArgEditField<Vector2>(vector2Args, "vector2Value", EditorGUI.Vector2Field);

                foreach (var param in parameters) {

                    //var valueRect = makeLabel(ObjectNames.NicifyVariableName(param.Name));
                    var valueRect = new Rect(position.x, position.y + position.height/controlCount * lineNumber++, position.width, position.height / controlCount) ;
                    var paramName = ObjectNames.NicifyVariableName(param.Name);
                    if (param.ParameterType.IsSubclassOf(typeof(UnityEngine.Object))) {
                        var argumentProperty = property.FindPropertyRelative(
                                "objref_arg_"+(++objRefParamCount));
                        argumentProperty.objectReferenceValue = EditorGUI.ObjectField(
                                valueRect, argumentProperty.objectReferenceValue, 
                                param.ParameterType, true);
                    } else {
                        var paramType = param.ParameterType;
                        if (paramType == typeof(string)) {
                            stringArgField(valueRect, paramName);
                        } else if (paramType == typeof(Color)) {
                            colorArgField(valueRect, paramName);
                        } else if (paramType == typeof(System.Int32)) {
                            intArgField(valueRect, paramName);
                        } else if (paramType == typeof(Vector2)) {
                            vector2ArgField(valueRect, paramName);
                        } else {
                            Debug.LogError("Method "+selectedMethod.Name + " not supported due to parameter type " + paramType.Name);
                        }
                    }

                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var target = getTarget(property);
            var methodProperty = property.FindPropertyRelative("method");
            int argCount = 0;
            var selectedMethod = target.GetBehaviourMethodByName(methodProperty.stringValue);
            if (selectedMethod != null) {
                argCount = selectedMethod.GetParameters().Length;
            }

            return base.GetPropertyHeight(property, label) * (3 + argCount);
        }
    }

}
