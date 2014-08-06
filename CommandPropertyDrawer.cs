using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Invocation {


    [CustomPropertyDrawer(typeof(Command))]
    public class CommandPropertyDrawer : PropertyDrawer
    {

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

            if (selectedMethod != null) {
                // Now we add edit fields for all parameters of selected method
                var parameters = selectedMethod.GetParameters();
                int objRefParamCount = 0, otherParamCount = 0;
                foreach (var param in parameters) {

                    var valueRect = makeLabel(ObjectNames.NicifyVariableName(param.Name));
                    if (param.ParameterType.IsSubclassOf(typeof(UnityEngine.Object))) {
                        var argumentProperty = property.FindPropertyRelative("objref_arg_"+(++objRefParamCount));
                        argumentProperty.objectReferenceValue = EditorGUI.ObjectField(valueRect, null, param.ParameterType, true);
                    } else {
                        var argumentProperty = property.FindPropertyRelative("value_arg_"+otherParamCount);
                        EditorGUI.BeginChangeCheck ();
                        string value = EditorGUI.TextField(valueRect, argumentProperty.stringValue);
                        if (EditorGUI.EndChangeCheck ()){
                            argumentProperty.stringValue = value;
                        }
                    }

                }
            }



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

            EditorGUI.indentLevel = indent;
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
