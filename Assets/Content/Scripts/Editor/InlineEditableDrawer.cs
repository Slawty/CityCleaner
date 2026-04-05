using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(InlineEditableAttribute))]
public class InlineEditableDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Draw foldout if class is complex
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            label,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            SerializedProperty prop = property.Copy();
            SerializedProperty end = prop.GetEndProperty();

            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            bool enterChildren = true;
            while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, end))
            {
                float height = EditorGUI.GetPropertyHeight(prop, true);
                Rect rect = new Rect(position.x, y, position.width, height);
                EditorGUI.PropertyField(rect, prop, true);
                y += height + 2;
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float totalHeight = EditorGUIUtility.singleLineHeight + 2; // foldout
        SerializedProperty prop = property.Copy();
        SerializedProperty end = prop.GetEndProperty();

        bool enterChildren = true;
        while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, end))
        {
            totalHeight += EditorGUI.GetPropertyHeight(prop, true) + 2;
            enterChildren = false;
        }

        return totalHeight;
    }
}

[CustomPropertyDrawer(typeof(InlineScriptableObjectAttribute))]
public class InlineScriptableObjectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Draw the ScriptableObject reference field first (drag & drop works)
        Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(objectFieldRect, property, label);

        if (property.objectReferenceValue == null)
            return;

        ScriptableObject so = property.objectReferenceValue as ScriptableObject;
        if (so == null)
            return;

        // Foldout to expand/collapse the inline properties
        Rect foldoutRect = new Rect(position.x + 10, position.y + EditorGUIUtility.singleLineHeight, position.width - 10, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);

        if (!property.isExpanded)
            return;

        // Draw all fields of the ScriptableObject inline
        SerializedObject soSerialized = new SerializedObject(so);
        soSerialized.Update();

        SerializedProperty prop = soSerialized.GetIterator();
        prop.NextVisible(true); // skip script reference field

        float yOffset = position.y + EditorGUIUtility.singleLineHeight * 2 + 2;

        while (prop.NextVisible(false))
        {
            float height = EditorGUI.GetPropertyHeight(prop, true);
            Rect rect = new Rect(position.x + 20, yOffset, position.width - 20, height);
            EditorGUI.PropertyField(rect, prop, true);
            yOffset += height + 2;
        }

        soSerialized.ApplyModifiedProperties();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // object field

        if (property.objectReferenceValue == null)
            return height;

        height += EditorGUIUtility.singleLineHeight; // foldout line

        if (!property.isExpanded)
            return height;

        ScriptableObject so = property.objectReferenceValue as ScriptableObject;
        if (so == null)
            return height;

        SerializedObject soSerialized = new SerializedObject(so);
        SerializedProperty prop = soSerialized.GetIterator();
        prop.NextVisible(true); // skip script field

        while (prop.NextVisible(false))
        {
            height += EditorGUI.GetPropertyHeight(prop, true) + 2;
        }

        height += 2; // extra padding
        return height;
    }
}