﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Tools.Extension;

namespace HandyControl.Controls
{
    [TemplatePart(Name = ElementItemsControl, Type = typeof(ItemsControl))]
    [TemplatePart(Name = ElementSearchBar, Type = typeof(SearchBar))]
    public class PropertyGrid : Control
    {
        private const string ElementItemsControl = "PART_ItemsControl";

        private const string ElementSearchBar = "PART_SearchBar";

        private ItemsControl _itemsControl;

        private ICollectionView _dataView;

        private SearchBar _searchBar;

        private string _searchKey;

        public PropertyGrid()
        {
            CommandBindings.Add(new CommandBinding(ControlCommands.SortByCategory, SortByCategory));
            CommandBindings.Add(new CommandBinding(ControlCommands.SortByName, SortByName));
        }

        public virtual PropertyResolver PropertyResolver { get; } = new PropertyResolver();

        public static readonly RoutedEvent SelectedObjectChangedEvent =
            EventManager.RegisterRoutedEvent("SelectedObjectChanged", RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<object>), typeof(PropertyGrid));

        public event RoutedPropertyChangedEventHandler<object> SelectedObjectChanged
        {
            add => AddHandler(SelectedObjectChangedEvent, value);
            remove => RemoveHandler(SelectedObjectChangedEvent, value);
        }

        public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
            "SelectedObject", typeof(object), typeof(PropertyGrid), new PropertyMetadata(default, OnSelectedObjectChanged));

        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (PropertyGrid)d;
            ctl.OnSelectedObjectChanged(e.OldValue, e.NewValue);
        }

        public object SelectedObject
        {
            get => GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        protected virtual void OnSelectedObjectChanged(object oldValue, object newValue)
        {
            UpdateItems(newValue);
            RaiseEvent(new RoutedPropertyChangedEventArgs<object>(oldValue, newValue, SelectedObjectChangedEvent));
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description", typeof(string), typeof(PropertyGrid), new PropertyMetadata(default(string)));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty MaxTitleWidthProperty = DependencyProperty.Register(
            "MaxTitleWidth", typeof(double), typeof(PropertyGrid), new PropertyMetadata(default(double)));

        public double MaxTitleWidth
        {
            get => (double) GetValue(MaxTitleWidthProperty);
            set => SetValue(MaxTitleWidthProperty, value);
        }

        public static readonly DependencyProperty MinTitleWidthProperty = DependencyProperty.Register(
            "MinTitleWidth", typeof(double), typeof(PropertyGrid), new PropertyMetadata(default(double)));

        public double MinTitleWidth
        {
            get => (double) GetValue(MinTitleWidthProperty);
            set => SetValue(MinTitleWidthProperty, value);
        }

        public override void OnApplyTemplate()
        {
            if (_searchBar != null)
            {
                _searchBar.SearchStarted -= SearchBar_SearchStarted;
            }

            base.OnApplyTemplate();

            _itemsControl = GetTemplateChild(ElementItemsControl) as ItemsControl;
            _searchBar = GetTemplateChild(ElementSearchBar) as SearchBar;

            if (_searchBar != null)
            {
                _searchBar.SearchStarted += SearchBar_SearchStarted;
            }

            UpdateItems(SelectedObject);
        }

        private void UpdateItems(object obj)
        {
            if (obj == null || _itemsControl == null) return;

            _dataView = CollectionViewSource.GetDefaultView(obj.GetType().GetProperties()
                .Where(item => PropertyResolver.ResolveBrowsable(item)).Select(CreatePropertyItem)
                .Do(item => item.InitElement()));

            SortByCategory(null, null);
            _itemsControl.ItemsSource = _dataView;
        }

        private void SortByCategory(object sender, ExecutedRoutedEventArgs e)
        {
            using (_dataView.DeferRefresh())
            {
                _dataView.GroupDescriptions.Clear();
                _dataView.SortDescriptions.Clear();
                _dataView.SortDescriptions.Add(new SortDescription(PropertyItem.CategoryProperty.Name, ListSortDirection.Ascending));
                _dataView.SortDescriptions.Add(new SortDescription(PropertyItem.PropertyNameProperty.Name, ListSortDirection.Ascending));
                _dataView.GroupDescriptions.Add(new PropertyGroupDescription(PropertyItem.CategoryProperty.Name));
            }
        }

        private void SortByName(object sender, ExecutedRoutedEventArgs e)
        {
            using (_dataView.DeferRefresh())
            {
                _dataView.GroupDescriptions.Clear();
                _dataView.SortDescriptions.Clear();
                _dataView.SortDescriptions.Add(new SortDescription(PropertyItem.PropertyNameProperty.Name, ListSortDirection.Ascending));
            }
        }

        private void SearchBar_SearchStarted(object sender, FunctionEventArgs<string> e)
        {
            _searchKey = e.Info;
            if (string.IsNullOrEmpty(_searchKey))
            {
                foreach (UIElement item in _dataView)
                {
                    item.Show();
                }
            }
            else
            {
                foreach (PropertyItem item in _dataView)
                {
                    item.Show(item.PropertyName.ToLower().Contains(_searchKey) || item.DisplayName.ToLower().Contains(_searchKey));
                }
            }
        }

        protected virtual PropertyItem CreatePropertyItem(PropertyInfo propertyInfo) => new PropertyItem
        {
            Category = PropertyResolver.ResolveCategory(propertyInfo),
            DisplayName = PropertyResolver.ResolveDisplayName(propertyInfo),
            Description = PropertyResolver.ResolveDescription(propertyInfo),
            IsReadOnly = PropertyResolver.ResolveIsReadOnly(propertyInfo),
            DefaultValue = PropertyResolver.ResolveDefaultValue(propertyInfo),
            Editor = PropertyResolver.ResolveEditor(propertyInfo),
            Converter = PropertyResolver.ResolveConverter(propertyInfo),
            Value = SelectedObject,
            PropertyName = propertyInfo.Name,
            PropertyTypeName = propertyInfo.PropertyType.FullName
        };

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            TitleElement.SetTitleWidth(this, new GridLength(Math.Max(MinTitleWidth, Math.Min(MaxTitleWidth, ActualWidth / 3))));
        }
    }
}