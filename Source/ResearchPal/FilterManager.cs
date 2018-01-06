﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace ResearchPal
{
    [StaticConstructorOnStartup]
    public class FilterManager
    {

        public enum FilterMatchType
        {
            NONE,
            NO_MATCH,
            RESEARCH,
            UNLOCK,
            TECH_LEVEL
        }

        #region Fields

        private const float _filterHeight = 24f;

        private string _filterPhrase = "";
        private bool _filterDirty = false;
        private bool _resetOnOpen = false;

        private string _keyChar = "";
        private bool _settingFocus = false;
        private bool _forceShowFilter = false;

        private string _filterResultTitle = "";
        private string _filterResultTooltip = "";
        private Dictionary<FilterMatchType, List<string>> _matchResults = new Dictionary<FilterMatchType, List<string>>();

        private bool _rectSet;
        private Rect _rectFilterBtn;
        private Rect _rectFilter;
        private Rect _rectClearBtn;
        private Rect _rectMessage;

        private const string _filterInputName = "FilterInput";

        public static Texture2D FilterIcon;
        #endregion Fields

        #region Constructors

        static FilterManager()
        {
            FilterIcon = ContentFinder<Texture2D>.Get("UI/Research/magnifier");
        }


        public FilterManager() {}

        #endregion Constructors

        #region Properties

        public float Height
        {
            get
            {
                return _filterHeight;
            }
        }

        public string PressedKeyChar
        {
            get
            {
                return _keyChar;
            }
            set
            {
                _keyChar = value;
            }

        }

        public Rect RectFilterBtn
        {
            get
            {
                if (!_rectSet)
                {
                    CreateRects();
                }
                return _rectFilterBtn;
            }
        }

        public Rect RectFilter
        {
            get
            {
                if (!_rectSet)
                {
                    CreateRects();
                }
                return _rectFilter;
            }
        }

        public Rect RectClearBtn
        {
            get
            {
                if (!_rectSet)
                {
                    CreateRects();
                }
                return _rectClearBtn;
            }
        }

        public Rect RectMessage
        {
            get
            {
                if (!_rectSet)
                {
                    CreateRects();
                }
                return _rectMessage;
            }
        }

        public string FilterPhrase
        {
            get
            {
                return _filterPhrase;
            }
            set
            {
                _filterPhrase = value;
            }
        }

        public bool FilterDirty
        {
            get
            {
                return _filterDirty || _resetOnOpen;
            }
        }

        #endregion Properties


        private void CreateRects()
        {
            // filter button
            _rectFilterBtn = new Rect(0f, 0f, _filterHeight, _filterHeight);

            // main filter area
            _rectFilter = new Rect(_rectFilterBtn.xMax + 6f, 0f, (UI.screenWidth - _rectFilterBtn.width) / 6f, _filterHeight);

            // clear button area
            _rectClearBtn = new Rect(_rectFilter.xMax + 3f, 0f + (_rectFilter.height - (_filterHeight / 2f)) / 2f, _filterHeight / 2f, _filterHeight / 2f);

            // result message area
            _rectMessage = new Rect(_rectClearBtn.xMax + 10f, 0f, UI.screenWidth - (_rectClearBtn.xMax + 10f), _filterHeight);

            _rectSet = true;
        }

        private bool FilterActive()
        {
            return (!_filterPhrase.NullOrEmpty() || _forceShowFilter);
        }

        private void ClearInput()
        {
            _filterPhrase = "";
            GUIUtility.keyboardControl = 0;
        }

        /// <summary>
        /// Compares a node's research, unlocks and tech-level to the current filter. Updates the node of it's match status.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public FilterMatchType NodeIsMatch(Node node)
        {
            if (!FilterDirty)
                return FilterMatchType.NONE;

            string phrase = _filterPhrase.Trim();
            FilterMatchType ret;
            if (phrase != "")
            {
                // look for matching research or matching recipes
                if (node.Research.label.Contains(phrase, StringComparison.InvariantCultureIgnoreCase))
                {
                    ret = FilterMatchType.RESEARCH;
                }
                else if (node.Research.GetUnlockDefsAndDescs() != null && 
                    node.Research.GetUnlockDefsAndDescs().Any(recipe => recipe.First.label.Contains(phrase, StringComparison.InvariantCulture)))
                {
                    ret = FilterMatchType.UNLOCK;
                } else if (node.Research.techLevel.ToStringHuman().Contains(phrase, StringComparison.InvariantCultureIgnoreCase))
                {
                    ret = FilterMatchType.TECH_LEVEL;
                } else {
                    ret = FilterMatchType.NO_MATCH;
                }
            } else {
                ret = FilterMatchType.NONE;
            }

            // save the result for display later
            if (ret.IsValidMatch())
            {
                if (!_matchResults.ContainsKey(ret))
                {
                    _matchResults.Add(ret, new List<string>());
                }
                _matchResults[ret].Add(node.Research.LabelCap);
            }

            // update the node of the result
            node.FilterMatch = ret;

            return ret;
        }

        public void DrawFilterControls(Rect canvas)
        {
            GUI.BeginGroup(canvas);

            string oldPhrase = _filterPhrase;
            if (_keyChar != "")
            {
                _filterPhrase = _keyChar;
                _keyChar = "";
            }            

            // check the toggle button
            if (Widgets.ButtonImage(RectFilterBtn, FilterIcon))
            {
                // flip the toggle
                _forceShowFilter = !(_forceShowFilter || !_filterPhrase.NullOrEmpty());
                if (!_forceShowFilter)
                {
                    ClearInput();
                }
            }

            if (FilterActive())
            {
                if (Widgets.ButtonImage(RectClearBtn, Widgets.CheckboxOffTex))
                {
                    ClearInput();
                } else {
                    // add a text widget with the current filter phrase
                    GUI.SetNextControlName(_filterInputName);

                    // focus the filter input field immediately if we're not already focused
                    if (GUI.GetNameOfFocusedControl() != _filterInputName)
                    {                        
                        _settingFocus = true;
                        GUI.FocusControl(_filterInputName);
                    } else {
                        // if the focus was just set, then the automatic behaviour is to select all the text
                        // we don't want that, so immediately deselect the text, and move the cursor to the end
                        if (_settingFocus)
                        {
                            TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                            te.SelectNone();
                            te.MoveTextEnd();
                            _settingFocus = false;
                        }
                    }
                    
                    _filterPhrase = Widgets.TextField(RectFilter, _filterPhrase);

                }
            } else {
                GUIUtility.keyboardControl = 0;
            }

            _filterDirty = (oldPhrase != _filterPhrase);
            if (_filterDirty)
            {
                _matchResults.Clear();
            }            
            GUI.EndGroup();
        }

        
        private void BuildFilterResultMessages()
        {
            
            _filterResultTitle = ResourceBank.String.FilterResults(_matchResults.Sum(k => k.Value.Count));

            var tt = new StringBuilder();
            foreach (KeyValuePair<FilterMatchType, List<string>> info in _matchResults.OrderBy(k => k.Key))
            {
                tt.AppendLine(info.Key.ToFriendlyString());
                tt.Append(string.Join(info.Value.Count() < 11 ? Environment.NewLine : ", ", info.Value.ToArray()));
                tt.AppendLine();
                tt.AppendLine();
            }
            _filterResultTooltip = tt.ToString();
        }

        public void DrawFilterResults(Rect canvas)
        {
            // rebuild the message/tooltip if necessary
            if (FilterDirty)
            {
                BuildFilterResultMessages();
            }

            // draw the 
            GUI.BeginGroup(canvas);
            if (FilterActive())
            {                
                Widgets.Label(RectMessage, _filterResultTitle);
                if (!_filterResultTooltip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(_rectMessage, _filterResultTooltip);
                }
            }
            GUI.EndGroup();
            _resetOnOpen = false;
        }

        public void Reset()
        {
            _filterPhrase = "";
            _forceShowFilter = false;
            _resetOnOpen = true;
        }

        public void CheckPressedKey()
        {
            if (Event.current.isKey && char.IsLetterOrDigit(Event.current.character) && GUI.GetNameOfFocusedControl() != _filterInputName)
            {
                _keyChar = Event.current.character.ToString();
            }
        }

    }

    public static class FilterMatchExtension
    {
        public static string ToFriendlyString(this FilterManager.FilterMatchType fType)
        {
            switch (fType)
            {
                case FilterManager.FilterMatchType.RESEARCH:
                    return ResourceBank.String.FilterTitleResearch;
                case FilterManager.FilterMatchType.UNLOCK:
                    return ResourceBank.String.FilterTitleUnlocks;
                case FilterManager.FilterMatchType.TECH_LEVEL:
                    return ResourceBank.String.FilterTitleTechLevel;
                default:
                    return "";
            }
        }

        public static bool IsValidMatch(this FilterManager.FilterMatchType fType)
        {
            return (fType > FilterManager.FilterMatchType.NO_MATCH);
        }
    }

}


