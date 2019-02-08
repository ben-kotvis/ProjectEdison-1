﻿using System;
using System.Threading.Tasks;

using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.App;
using Android.Support.V4.Content.Res;
using Android.Support.V7.Widget;
using Android.Support.V7.View.Menu;

using Edison.Mobile.Android.Common;
using Android.Util;
using System.Collections.Generic;

namespace Edison.Mobile.User.Client.Droid
{
    public static class Constants
    {
        public const string ClientId = "19cb746c-3066-4cd8-8cd2-e0ce1176ae33"; //"64531b8c-3d22-4c2a-8d72-bf37c8609fbe";

        // Response summary Map values
        public const int UserLocationJitterThreshold = 3; //meters
        public const int SingleLocationRefocusMapThreshold = 5000; // meters
        public const float LocationThresholdPercent = 0.1f; // % as fraction


        internal const string CHANNEL_ID = "my_notification_channel";
        internal const int NOTIFICATION_ID = 100;

        public static int StatusBarHeightPx = PixelSizeConverter.DpToPx(24);

        public static int ToolbarHeightPx = -1;

        public static int AvailablePageHeightPx = -1;

        public static int BottomSheetPeekHeightPx { get; private set; } = -1;
        public static int BottomSheetHeightPx { get; private set; } = -1;
        public static int BottomSheetThumbTotalHeightPx { get; private set; } = -1;
        public static int BottomSheetContentHeightPx { get; private set; } = -1;


        public static Padding PagePaddingPx { get; private set; }

        public static int EventGaugeAreaHeightPx { get; private set; } = -1;
        public static int EventGaugeSizePx { get; private set; } = -1;

        public static int EventResponseAreaHeightPx { get; private set; } = -1;
        public static int EventResponseCardWidthPx { get; private set; } = -1;
        public static int EventResponseCardSeperatorWidthPx { get; private set; } = -1;


        public static int BrightnessContainerWidth { get; private set; } = -1;
        public static int BrightnessToolbarItemIconBottomPadding { get; private set; } = -1;



        public readonly static Color DefaultResponseColor = Color.Argb(255, 34, 240, 255);

        public const float DefaultResponseMapZoom = 5f;





        public static async Task CalculateUIDimensionsAsync(Activity act)
        {
            await Task.Run(() =>
            {
                CalculateUIDimensions(act);
                return;
            }).ConfigureAwait(false);
        }
        public static void CalculateUIDimensions(Activity act)
        {

            var displayHeightPx = DisplayDetails.DisplayHeightPx;
            var displayWidthPx = DisplayDetails.DisplayWidthPx;

            // Calculate current bar heights
            UpdateBarDimensions(act);

            // BottomSheet PeekHeight
            int quickChatIconHorizontalMarginPx =
                (int)act.Resources.GetDimension(Resource.Dimension.bottom_sheet_button_icon_padding);
            int quickChatIconVerticalMarginsPx = quickChatIconHorizontalMarginPx +
                                                 (int)act.Resources.GetDimension(Resource.Dimension
                                                     .bottom_sheet_button_icon_toppadding);
            int labelHeightPx = act.Resources.GetDimensionPixelSize(Resource.Dimension.bottom_sheet_button_text_size);
            int bottomSheetThumbHeightPx =
                2 * (int)act.Resources.GetDimension(Resource.Dimension.bottom_sheet_thumb_padding) +
                (int)act.Resources.GetDimension(Resource.Dimension.bottom_sheet_thumb_height);
            int quickChatIconHorizontalMarginsPx = 2 * quickChatIconHorizontalMarginPx;
            int quickChatIconDiameterPx = (int)(displayWidthPx / 3 - quickChatIconHorizontalMarginsPx);
            BottomSheetPeekHeightPx = quickChatIconDiameterPx + labelHeightPx + bottomSheetThumbHeightPx +
                                      quickChatIconVerticalMarginsPx;
            // BottomSheet Height
            BottomSheetHeightPx = displayHeightPx - act.Resources.GetDimensionPixelSize(Resource.Dimension.abc_action_bar_default_height_material);
            BottomSheetThumbTotalHeightPx = act.Resources.GetDimensionPixelSize(Resource.Dimension.bottom_sheet_thumb_height) +
                                                2 * act.Resources.GetDimensionPixelSize(Resource.Dimension.bottom_sheet_thumb_padding);
            BottomSheetContentHeightPx = BottomSheetHeightPx - BottomSheetThumbTotalHeightPx;

            PagePaddingPx = new Padding(0, 0, 0, BottomSheetPeekHeightPx);

            AvailablePageHeightPx = displayHeightPx - StatusBarHeightPx - ToolbarHeightPx - BottomSheetPeekHeightPx;

            // Home Page dimensions
            EventGaugeAreaHeightPx = (int)Math.Round((double)(2 * AvailablePageHeightPx / 5));
            EventResponseAreaHeightPx = AvailablePageHeightPx - EventGaugeAreaHeightPx;
            // Need to use this directly as can be a race condition issue (or Xamarin bug) when adjusting CircularEventGauge size via OnSizeChanged
            EventGaugeSizePx = EventGaugeAreaHeightPx - 2 * act.Resources.GetDimensionPixelSize(Resource.Dimension.event_guage_area_padding);

            EventResponseCardWidthPx = (int)(displayWidthPx * 0.65);
            EventResponseCardSeperatorWidthPx = (int)((displayWidthPx - EventResponseCardWidthPx) / 2);


        }

        public static async Task UpdateBarDimensionsAsync(Activity act)
        {
            await Task.Run(() =>
            {
                UpdateBarDimensions(act);
                return;
            }).ConfigureAwait(false);
        }
        public static void UpdateBarDimensions(Activity act)
        {
            Rect displayFrame = new Rect();
            act.Window.DecorView.GetWindowVisibleDisplayFrame(displayFrame);
            var statusBarHeight = displayFrame.Top;
            if (statusBarHeight > 0)
                StatusBarHeightPx = statusBarHeight;

            int contentViewTop = act.Window.FindViewById(Window.IdAndroidContent).Top;
            int titleBarHeight = contentViewTop - statusBarHeight;
            ToolbarHeightPx = (int)act.Resources.GetDimension(Resource.Dimension.abc_action_bar_default_height_material);
            if (titleBarHeight > ToolbarHeightPx)
                ToolbarHeightPx = titleBarHeight;
        }


        public static void UpdateBrightnessControlDimensions(Toolbar toolbar, ActionMenuItemView menuItem)
        {
            // Get the position of the menu item
            int[] itemLocation = new int[2];
            menuItem?.GetLocationInWindow(itemLocation);
            // Get the position of the toolbar
            int[] toolbarLocation = new int[2];
            toolbar.GetLocationInWindow(toolbarLocation);
            // Calculate the horizontal center position of the menu item
            var itemCenterX = itemLocation[0] - toolbarLocation[0] + menuItem.Width / 2;
            // Get or calculate the distance between the bottom of the menu item icon and the bottom of the toolbar
            // NOTE: TotalPaddingBottom is actually slightly less than toolbar height - icon height / 2, however it automatically handles if the icon gravity has been modified
            BrightnessToolbarItemIconBottomPadding = menuItem.TotalPaddingBottom;
            // int itemBottomPadding = (_toolbar.Height - menuItem.ItemData.Icon.Bounds.Height())/2;
            // Calculate the width of the brightness control container required top center under the menu item
            BrightnessContainerWidth = 2 * (toolbar.Width - itemCenterX);


        }

        /*
                public static void CalculateDefaultZoms(Context ctx)
                {
                    TypedValue typedValue = new TypedValue();

                    ctx.Resources.GetValue(Resource.Dimension.reponse_card_map_default_zoom, typedValue, true);
                    DefaultResponseMapZoom = typedValue.Float;

                }
        */

        private static readonly bool _eventRedSet = false;
        private static Color _eventRed;
        private static readonly bool _eventYellowSet = false;
        private static Color _eventYellow;
        private static readonly bool _eventBlueSet = false;
        private static Color _eventBlue;
        public static Color GetEventTypeColor(Context ctx, string colorName)
        {
            switch (colorName)
            {
                case Core.Shared.Constants.ColorName.Red:
                    if (!_eventRedSet)
                        _eventRed = new Color(ResourcesCompat.GetColor(ctx.Resources, Resource.Color.icon_red, null));
                    return _eventRed;
                case Core.Shared.Constants.ColorName.Yellow:
                    if (!_eventYellowSet)
                        _eventYellow = new Color(ResourcesCompat.GetColor(ctx.Resources, Resource.Color.app_yellow, null));
                    return _eventYellow;
                case Core.Shared.Constants.ColorName.Blue:
                    if (!_eventBlueSet)
                        _eventBlue = new Color(ResourcesCompat.GetColor(ctx.Resources, Resource.Color.icon_blue, null));
                    return _eventBlue;
                default:
                    if (!_eventBlueSet)
                        _eventBlue = new Color(ResourcesCompat.GetColor(ctx.Resources, Resource.Color.icon_blue, null));
                    return _eventBlue;
            }
        }

        public static readonly Dictionary<string, string> ChatMessageButtonNameToColorMap = new Dictionary<string, string>()
        {
            { "Fire", Core.Shared.Constants.ColorName.Red},
            { "fire", Core.Shared.Constants.ColorName.Red},
            { "gun", Core.Shared.Constants.ColorName.Red},
            { "Gun", Core.Shared.Constants.ColorName.Red},
            { "Active Shooter", Core.Shared.Constants.ColorName.Red},
            { "health", Core.Shared.Constants.ColorName.Blue},
            { "Health", Core.Shared.Constants.ColorName.Blue},
            { "Health Check", Core.Shared.Constants.ColorName.Blue},
            { "pollution", Core.Shared.Constants.ColorName.Blue},
            { "Pollution", Core.Shared.Constants.ColorName.Blue},
            { "Air Quality", Core.Shared.Constants.ColorName.Blue},
            { "protest", Core.Shared.Constants.ColorName.Blue},
            { "Protest", Core.Shared.Constants.ColorName.Blue},
            { "package", Core.Shared.Constants.ColorName.Red},
            { "Package", Core.Shared.Constants.ColorName.Red},
            { "Suspicious Package", Core.Shared.Constants.ColorName.Red},
            { "tornado", Core.Shared.Constants.ColorName.Yellow},
            { "Tornado", Core.Shared.Constants.ColorName.Yellow},
            { "vip", Core.Shared.Constants.ColorName.Blue},
            { "Vip", Core.Shared.Constants.ColorName.Blue},
            { "VIP", Core.Shared.Constants.ColorName.Blue},
            { "emergency", Core.Shared.Constants.ColorName.Red},
            { "Emergency", Core.Shared.Constants.ColorName.Red}
        };

        public static readonly Dictionary<string, string> ChatMessageButtonNameToIconMap = new Dictionary<string, string>()
        {
            { "Fire", "fire"},
            { "fire", "fire"},
            { "gun", "gun"},
            { "Gun", "gun"},
            { "Active Shooter", "gun"},
            { "health", "health_check"},
            { "Health", "health_check"},
            { "Health Check", "health_check"},
            { "pollution", "air_quality"},
            { "Pollution", "air_quality"},
            { "Air Quality", "air_quality"},
            { "protest", "protest"},
            { "Protest", "protest"},
            { "package", "suspicious_package"},
            { "Package", "suspicious_package"},
            { "Suspicious Package", "suspicious_package"},
            { "tornado", "tornado"},
            { "Tornado", "tornado"},
            { "vip", "vip"},
            { "Vip", "vip"},
            { "VIP", "vip"},
            { "emergency", "emergency"},
            { "Emergency", "emergency"}
        };



        public static Tuple<string, Color> GetChatMessageButtonSettings(Context ctx, string name, string fallbackIconName = null)
        {
            string iconName;
            if (!ChatMessageButtonNameToIconMap.TryGetValue(name, out iconName))
            {
                if (!string.IsNullOrWhiteSpace(fallbackIconName) && !ChatMessageButtonNameToIconMap.TryGetValue(fallbackIconName, out iconName))
                    iconName = fallbackIconName;
            }
            if (string.IsNullOrWhiteSpace(iconName))
                iconName = "emergency";
            string colorName;
            if (!ChatMessageButtonNameToColorMap.TryGetValue(name, out colorName))
            {
                if (!ChatMessageButtonNameToColorMap.TryGetValue(fallbackIconName, out colorName))
                    colorName = Core.Shared.Constants.ColorName.Blue;
            }
            return new Tuple<string, Color>(iconName, GetEventTypeColor(ctx, colorName));
        }

    }
}

