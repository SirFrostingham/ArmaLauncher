using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;

namespace ArmaLauncher.Helpers
{

    public static class Util
    {
        public static T StringToEnum<T>(string name)
        {
            return (T)Enum.Parse(typeof(T), name);
        }

        public static string ToDescriptionString(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            var attributesLocalizable = (LocalizableDescriptionAttribute[])fi.GetCustomAttributes(typeof(LocalizableDescriptionAttribute), false);

            var allowedAttributes = (from descriptionAttribute in attributes 
                                        from localizableDescriptionAttribute in attributesLocalizable 
                                        where descriptionAttribute.Description != localizableDescriptionAttribute.Description 
                                     select descriptionAttribute).ToArray();

            if (allowedAttributes.Length > 0)
                return allowedAttributes[0].Description;
            
            return value.ToString();
        }

        /// <summary>
        /// Used specifically in EnumDescriptionConverter
        /// </summary>
        /// <param name="enumObj"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute attrib = attribArray[0] as DescriptionAttribute;
                return attrib.Description;
            }
        } 
    }
}
