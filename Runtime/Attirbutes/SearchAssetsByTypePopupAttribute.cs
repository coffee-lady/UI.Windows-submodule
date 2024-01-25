using System;
using System.Collections;

namespace UnityEngine.UI.Windows.Utilities
{
    public class SearchAssetsByTypePopupAttribute : PropertyAttribute
    {
        public Type filterType;
        public string filterDir;
        public string menuName;
        public string innerField;
        public string noneOption;

        public SearchAssetsByTypePopupAttribute(Type filterType = null, string filterDir = null, string menuName = null,
            string innerField = null, string noneOption = "None")
        {
            this.filterType = filterType;
            this.filterDir = filterDir;
            this.menuName = menuName;
            this.innerField = innerField;
            this.noneOption = noneOption;
        }
    }

    public interface ISearchComponentByTypeEditor
    {
        Type GetSearchType();
    }

    public interface ISearchComponentByTypeSingleEditor
    {
        IList GetSearchTypeArray();
    }

    public class SearchComponentsByTypePopupAttribute : PropertyAttribute
    {
        public Type baseType;
        public string menuName;
        public string innerField;
        public string noneOption;
        public bool allowClassOverrides;
        public bool singleOnly;

        public SearchComponentsByTypePopupAttribute(Type baseType, string menuName = null, string innerField = null,
            string noneOption = "None", bool allowClassOverrides = false, bool singleOnly = false)
        {
            this.baseType = baseType;
            this.menuName = menuName;
            this.innerField = innerField;
            this.noneOption = noneOption;
            this.allowClassOverrides = allowClassOverrides;
            this.singleOnly = singleOnly;
        }
    }
}