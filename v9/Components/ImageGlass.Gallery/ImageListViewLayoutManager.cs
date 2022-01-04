﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2022 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

---------------------
ImageGlass.Gallery is based on ImageListView v13.8.2:
Url: https://github.com/oozcitak/imagelistview
License: Apache License Version 2.0, http://www.apache.org/licenses/
---------------------
*/

namespace ImageGlass.Gallery;


/// <summary>
/// Represents the layout of the image list view drawing area.
/// </summary>
internal class ImageListViewLayoutManager
{
    #region Member Variables
    private Rectangle mClientArea;
    private ImageListView mImageListView;
    private Rectangle mItemAreaBounds;
    private Size mItemSize;
    private Size mItemSizeWithMargin;
    private int mDisplayedCols;
    private int mDisplayedRows;
    private int mItemCols;
    private int mItemRows;
    private int mFirstPartiallyVisible;
    private int mLastPartiallyVisible;
    private int mFirstVisible;
    private int mLastVisible;

    private View cachedView;
    private Point cachedViewOffset;
    private Size cachedSize;
    private int cachedItemCount;
    private Size cachedItemSize;
    private bool cachedIntegralScroll;
    private Size cachedItemMargin;
    private bool cachedScrollBars;
    private Dictionary<Guid, bool> cachedVisibleItems;

    private bool vScrollVisible;
    private bool hScrollVisible;

    // Size required to display all items (i.e. scroll range)
    private int totalWidth;
    private int totalHeight;
    #endregion


    #region Properties
    /// <summary>
    /// Gets the bounds of the entire client area.
    /// </summary>
    public Rectangle ClientArea { get { return mClientArea; } }
    
    /// <summary>
    /// Gets the owner image list view.
    /// </summary>
    public ImageListView ImageListView { get { return mImageListView; } }
    
    /// <summary>
    /// Gets the extends of the item area.
    /// </summary>
    public Rectangle ItemAreaBounds { get { return mItemAreaBounds; } }
    
    /// <summary>
    /// Gets the items size.
    /// </summary>
    public Size ItemSize { get { return mItemSize; } }
    
    /// <summary>
    /// Gets the items size including the margin around the item.
    /// </summary>
    public Size ItemSizeWithMargin { get { return mItemSizeWithMargin; } }
    
    /// <summary>
    /// Gets the maximum number of columns that can be displayed.
    /// </summary>
    public int Cols { get { return mDisplayedCols; } }
    
    /// <summary>
    /// Gets the maximum number of rows that can be displayed.
    /// </summary>
    public int Rows { get { return mDisplayedRows; } }
    
    /// <summary>
    /// Gets the index of the first partially visible item.
    /// </summary>
    public int FirstPartiallyVisible { get { return mFirstPartiallyVisible; } }
    
    /// <summary>
    /// Gets the index of the last partially visible item.
    /// </summary>
    public int LastPartiallyVisible { get { return mLastPartiallyVisible; } }
    
    /// <summary>
    /// Gets the index of the first fully visible item.
    /// </summary>
    public int FirstVisible { get { return mFirstVisible; } }
    
    /// <summary>
    /// Gets the index of the last fully visible item.
    /// </summary>
    public int LastVisible { get { return mLastVisible; } }
    
    /// <summary>
    /// Determines whether an update is required.
    /// </summary>
    public bool UpdateRequired
    {
        get
        {
            if (mImageListView.View != cachedView)
                return true;
            else if (mImageListView.ViewOffset != cachedViewOffset)
                return true;
            else if (mImageListView.ClientSize != cachedSize)
                return true;
            else if (mImageListView.Items.Count != cachedItemCount)
                return true;
            else if (mImageListView.mRenderer.MeasureItem(mImageListView.View) != cachedItemSize)
                return true;
            else if (mImageListView.mRenderer.MeasureItemMargin(mImageListView.View) != cachedItemMargin)
                return true;
            else if (mImageListView.ScrollBars != cachedScrollBars)
                return true;
            else if (mImageListView.IntegralScroll != cachedIntegralScroll)
                return true;
            else if (mImageListView.Items.collectionModified)
                return true;
            else
                return false;
        }
    }

    #endregion


    #region Constructor
    /// <summary>
    /// Initializes a new instance of the ImageListViewLayoutManager class.
    /// </summary>
    /// <param name="owner">The owner control.</param>
    public ImageListViewLayoutManager(ImageListView owner)
    {
        mImageListView = owner;
        cachedVisibleItems = new Dictionary<Guid, bool>();

        vScrollVisible = false;
        hScrollVisible = false;

        Update();
    }

    #endregion


    #region Instance Methods
    /// <summary>
    /// Determines whether the item with the given guid is
    /// (partially) visible.
    /// </summary>
    /// <param name="guid">The guid of the item to check.</param>
    public bool IsItemVisible(Guid guid)
    {
        return cachedVisibleItems.ContainsKey(guid);
    }

    /// <summary>
    /// Determines whether the item with the given guid is partially visible.
    /// </summary>
    /// <param name="index">The index of the item to check.</param>
    /// <returns></returns>
    public bool IsItemPartialyVisible(int index)
    {
        return index == FirstPartiallyVisible || index == LastPartiallyVisible;
    }

    /// <summary>
    /// Returns the bounds of the item with the specified index.
    /// </summary>
    public Rectangle GetItemBounds(int itemIndex)
    {
        var location = mItemAreaBounds.Location;
        location.X += cachedItemMargin.Width / 2 - mImageListView.ViewOffset.X;
        location.Y += cachedItemMargin.Height / 2 - mImageListView.ViewOffset.Y;

        if (ImageListView.View == View.HorizontalStrip)
            location.X += itemIndex * mItemSizeWithMargin.Width;
        else
        {
            location.X += (itemIndex % mDisplayedCols) * mItemSizeWithMargin.Width;
            location.Y += (itemIndex / mDisplayedCols) * mItemSizeWithMargin.Height;
        }

        return new Rectangle(location, mItemSize);
    }

    /// <summary>
    /// Returns the bounds of the item with the specified index, 
    /// including the margin around the item.
    /// </summary>
    public Rectangle GetItemBoundsWithMargin(int itemIndex)
    {
        Rectangle rec = GetItemBounds(itemIndex);
        rec.Inflate(cachedItemMargin.Width / 2, cachedItemMargin.Height / 2);
        return rec;
    }

    /// <summary>
    /// Returns the item checkbox bounds.
    /// This method assumes a checkbox icon size of 16x16
    /// </summary>
    public Rectangle GetCheckBoxBounds(int itemIndex)
    {
        Rectangle bounds = GetWidgetBounds(GetItemBounds(itemIndex), new Size(16, 16),
            mImageListView.CheckBoxPadding, mImageListView.CheckBoxAlignment);

        // If the checkbox and the icon have the same alignment,
        // move the checkbox horizontally away from the icon
        if (mImageListView.CheckBoxAlignment == mImageListView.IconAlignment &&  mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
        {
            ContentAlignment alignment = mImageListView.CheckBoxAlignment;
            if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
                bounds.X -= 8 + mImageListView.IconPadding.Width / 2;
            else if (alignment == ContentAlignment.BottomRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.TopRight)
                bounds.X -= 16 + mImageListView.IconPadding.Width;
        }

        return bounds;
    }

    /// <summary>
    /// Returns the item icon bounds.
    /// This method assumes an icon size of 16x16
    /// </summary>
    public Rectangle GetIconBounds(int itemIndex)
    {
        Rectangle bounds = GetWidgetBounds(GetItemBounds(itemIndex), new Size(16, 16),
            mImageListView.IconPadding, mImageListView.IconAlignment);

        // If the checkbox and the icon have the same alignment,
        // or in details view move the icon horizontally away from the checkbox
        if (mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
            bounds.X += 16 + 2;
        else if (mImageListView.CheckBoxAlignment == mImageListView.IconAlignment &&
            mImageListView.ShowCheckBoxes && mImageListView.ShowFileIcons)
        {
            ContentAlignment alignment = mImageListView.CheckBoxAlignment;
            if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.TopLeft)
                bounds.X += 16 + mImageListView.IconPadding.Width;
            else if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
                bounds.X += 8 + mImageListView.IconPadding.Width / 2;
        }

        return bounds;
    }

    /// <summary>
    /// Returns the bounds of a widget.
    /// Used to calculate the bounds of checkboxes and icons.
    /// </summary>
    private Rectangle GetWidgetBounds(Rectangle bounds, Size size, Size padding, ContentAlignment alignment)
    {
        // Apply padding
        bounds.Inflate(-padding.Width, -padding.Height);

        int x = 0;
        if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.TopLeft)
            x = bounds.Left;
        else if (alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.TopCenter)
            x = bounds.Left + bounds.Width / 2 - size.Width / 2;
        else // if (alignment == ContentAlignment.BottomRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.TopRight)
            x = bounds.Right - size.Width;

        int y = 0;
        if (alignment == ContentAlignment.BottomLeft || alignment == ContentAlignment.BottomCenter || alignment == ContentAlignment.BottomRight)
            y = bounds.Bottom - size.Height;
        else if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight)
            y = bounds.Top + bounds.Height / 2 - size.Height / 2;
        else // if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
            y = bounds.Top;

        return new Rectangle(x, y, size.Width, size.Height);
    }

    /// <summary>
    /// Recalculates the control layout.
    /// </summary>
    public void Update()
    {
        Update(false);
    }

    /// <summary>
    /// Recalculates the control layout.
    /// <param name="forceUpdate">true to force an update; otherwise false.</param>
    /// </summary>
    public void Update(bool forceUpdate)
    {
        if (mImageListView.ClientRectangle.Width == 0 || mImageListView.ClientRectangle.Height == 0)
            return;

        // If only item order is changed, just update visible items.
        if (!forceUpdate && !UpdateRequired && mImageListView.Items.collectionModified)
        {
            UpdateVisibleItems();
            return;
        }

        if (!forceUpdate && !UpdateRequired)
            return;

        // Get the item size from the renderer
        mItemSize = mImageListView.mRenderer.MeasureItem(mImageListView.View);
        cachedItemMargin = mImageListView.mRenderer.MeasureItemMargin(mImageListView.View);
        mItemSizeWithMargin = mItemSize + cachedItemMargin;

        // Cache current properties to determine if we will need an update later
        bool viewChanged = (cachedView != mImageListView.View);
        cachedView = mImageListView.View;
        cachedViewOffset = mImageListView.ViewOffset;
        cachedSize = mImageListView.ClientSize;
        cachedItemCount = mImageListView.Items.Count;
        cachedIntegralScroll = mImageListView.IntegralScroll;
        cachedItemSize = mItemSize;
        cachedScrollBars = mImageListView.ScrollBars;
        mImageListView.Items.collectionModified = false;

        // Calculate item area bounds
        if (!UpdateItemArea())
            return;

        // Let the calculated bounds modified by the renderer
        LayoutEventArgs eLayout = new LayoutEventArgs(mItemAreaBounds);
        mImageListView.mRenderer.OnLayout(eLayout);
        mItemAreaBounds = eLayout.ItemAreaBounds;
        if (mItemAreaBounds.Width <= 0 || mItemAreaBounds.Height <= 0)
            return;

        // Calculate the number of rows and columns
        CalculateGrid();

        // Check if we need the scroll bars.
        // Recalculate the layout if scroll bar visibility changes.
        if (CheckScrollBars())
        {
            Update(true);
            return;
        }

        // Update scroll range
        UpdateScrollBars();

        // Cache visible items
        UpdateVisibleItems();

        // Recalculate the layout if view mode was changed
        if (viewChanged)
            Update();
    }

    /// <summary>
    /// Calculates the maximum number of rows and columns 
    /// that can be fully displayed.
    /// </summary>
    private void CalculateGrid()
    {
        // Number of rows and columns shown on screen
        mDisplayedRows = (int)Math.Floor(mItemAreaBounds.Height / (float)mItemSizeWithMargin.Height);
        mDisplayedCols = (int)
            Math.Floor(mItemAreaBounds.Width / (float)mItemSizeWithMargin.Width);

        if (mImageListView.View == View.VerticalStrip) mDisplayedCols = 1;
        if (mImageListView.View == View.HorizontalStrip) mDisplayedRows = 1;
        if (mDisplayedCols < 1) mDisplayedCols = 1;
        if (mDisplayedRows < 1) mDisplayedRows = 1;

        // Number of rows and columns to enclose all items
        if (mImageListView.View == View.HorizontalStrip)
        {
            mItemRows = mDisplayedRows;
            mItemCols = (int)Math.Ceiling(mImageListView.Items.Count / (float)mDisplayedRows);
        }
        else
        {
            mItemCols = mDisplayedCols;
            mItemRows = (int)Math.Ceiling(mImageListView.Items.Count / (float)mDisplayedCols);
        }

        totalWidth = mItemCols * mItemSizeWithMargin.Width;
        totalHeight = mItemRows * mItemSizeWithMargin.Height;
    }

    /// <summary>
    /// Calculates the item area.
    /// </summary>
    /// <returns>true if the item area is not empty (both width and height
    /// greater than zero); otherwise false.</returns>
    private bool UpdateItemArea()
    {
        // Calculate drawing area
        mClientArea = mImageListView.ClientRectangle;
        if (mImageListView.BorderStyle != System.Windows.Forms.BorderStyle.None)
            mClientArea.Inflate(-1, -1);
        mItemAreaBounds = mClientArea;

        // Allocate space for scrollbars
        if (mImageListView.hScrollBar.Visible)
        {
            mClientArea.Height -= mImageListView.hScrollBar.Height;
            mItemAreaBounds.Height -= mImageListView.hScrollBar.Height;
        }
        if (mImageListView.vScrollBar.Visible)
        {
            mClientArea.Width -= mImageListView.vScrollBar.Width;
            mItemAreaBounds.Width -= mImageListView.vScrollBar.Width;
        }

        return mItemAreaBounds.Width > 0 && mItemAreaBounds.Height > 0;
    }

    /// <summary>
    /// Shows or hides the scroll bars.
    /// Returns true if the layout needs to be recalculated; otherwise false.
    /// </summary>
    /// <returns></returns>
    private bool CheckScrollBars()
    {
        // Horizontal scroll bar
        bool hScrollRequired = false;
        bool hScrollChanged = false;
        if (mImageListView.ScrollBars)
            hScrollRequired = (mImageListView.Items.Count > 0) && (mItemAreaBounds.Width < totalWidth);

        if (hScrollRequired != hScrollVisible)
        {
            hScrollVisible = hScrollRequired;
            mImageListView.hScrollBar.Visible = hScrollRequired;
            hScrollChanged = true;
        }

        // Vertical scroll bar
        bool vScrollRequired = false;
        bool vScrollChanged = false;
        if (mImageListView.ScrollBars)
            vScrollRequired = (mImageListView.Items.Count > 0) && (mItemAreaBounds.Height < totalHeight);

        if (vScrollRequired != vScrollVisible)
        {
            vScrollVisible = vScrollRequired;
            mImageListView.vScrollBar.Visible = vScrollRequired;
            vScrollChanged = true;
        }

        // Determine if the layout needs to be recalculated
        return (hScrollChanged || vScrollChanged);
    }

    /// <summary>
    /// Updates scroll bar parameters.
    /// </summary>
    private void UpdateScrollBars()
    {
        // Set scroll range
        if (mImageListView.Items.Count != 0)
        {
            // Horizontal scroll range
            if (mImageListView.ScrollOrientation == System.Windows.Forms.ScrollOrientation.HorizontalScroll)
            {
                mImageListView.hScrollBar.Minimum = 0;
                mImageListView.hScrollBar.Maximum = Math.Max(0, totalWidth - 1);
                if (!mImageListView.IntegralScroll)
                    mImageListView.hScrollBar.LargeChange = mItemAreaBounds.Width;
                else
                    mImageListView.hScrollBar.LargeChange = mItemSizeWithMargin.Width * mDisplayedCols;
                mImageListView.hScrollBar.SmallChange = mItemSizeWithMargin.Width;
            }
            else
            {
                mImageListView.hScrollBar.Minimum = 0;
                mImageListView.hScrollBar.Maximum = mDisplayedCols * mItemSizeWithMargin.Width;
                mImageListView.hScrollBar.LargeChange = mItemAreaBounds.Width;
                mImageListView.hScrollBar.SmallChange = 1;
            }
            if (mImageListView.ViewOffset.X > mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1)
            {
                mImageListView.hScrollBar.Value = mImageListView.hScrollBar.Maximum - mImageListView.hScrollBar.LargeChange + 1;
                mImageListView.ViewOffset = new Point(mImageListView.hScrollBar.Value, mImageListView.ViewOffset.Y);
            }

            // Vertical scroll range
            if (mImageListView.ScrollOrientation == System.Windows.Forms.ScrollOrientation.HorizontalScroll)
            {
                mImageListView.vScrollBar.Minimum = 0;
                mImageListView.vScrollBar.Maximum = mDisplayedRows * mItemSizeWithMargin.Height;
                mImageListView.vScrollBar.LargeChange = mItemAreaBounds.Height;
                mImageListView.vScrollBar.SmallChange = 1;
            }
            else
            {
                mImageListView.vScrollBar.Minimum = 0;
                mImageListView.vScrollBar.Maximum = Math.Max(0, totalHeight - 1);
                if (!mImageListView.IntegralScroll)
                    mImageListView.vScrollBar.LargeChange = mItemAreaBounds.Height;
                else
                    mImageListView.vScrollBar.LargeChange = mItemSizeWithMargin.Height * mDisplayedRows;
                mImageListView.vScrollBar.SmallChange = mItemSizeWithMargin.Height;
            }
            if (mImageListView.ViewOffset.Y > mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1)
            {
                mImageListView.vScrollBar.Value = mImageListView.vScrollBar.Maximum - mImageListView.vScrollBar.LargeChange + 1;
                mImageListView.ViewOffset = new Point(mImageListView.ViewOffset.X, mImageListView.vScrollBar.Value);
            }
        }
        else // if (mImageListView.Items.Count == 0)
        {
            // Zero out the scrollbars if we don't have any items
            mImageListView.hScrollBar.Minimum = 0;
            mImageListView.hScrollBar.Maximum = 0;
            mImageListView.hScrollBar.Value = 0;
            mImageListView.vScrollBar.Minimum = 0;
            mImageListView.vScrollBar.Maximum = 0;
            mImageListView.vScrollBar.Value = 0;
            mImageListView.ViewOffset = new Point(0, 0);
        }

        Rectangle bounds = mImageListView.ClientRectangle;
        if (mImageListView.BorderStyle != System.Windows.Forms.BorderStyle.None)
            bounds.Inflate(-1, -1);

        // Horizontal scrollbar position
        mImageListView.hScrollBar.Left = bounds.Left;
        mImageListView.hScrollBar.Top = bounds.Bottom - mImageListView.hScrollBar.Height;
        mImageListView.hScrollBar.Width = bounds.Width - (mImageListView.vScrollBar.Visible ? mImageListView.vScrollBar.Width : 0);
        // Vertical scrollbar position
        mImageListView.vScrollBar.Left = bounds.Right - mImageListView.vScrollBar.Width;
        mImageListView.vScrollBar.Top = bounds.Top;
        mImageListView.vScrollBar.Height = bounds.Height - (mImageListView.hScrollBar.Visible ? mImageListView.hScrollBar.Height : 0);
    }

    /// <summary>
    /// Updates the dictionary of visible items.
    /// </summary>
    private void UpdateVisibleItems()
    {
        // Find the first and last visible items
        if (mImageListView.View == View.HorizontalStrip)
        {
            mFirstPartiallyVisible = (int)Math.Floor(mImageListView.ViewOffset.X / (float)mItemSizeWithMargin.Width) * mDisplayedRows;
            mLastPartiallyVisible = (int)Math.Ceiling((mImageListView.ViewOffset.X + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * mDisplayedRows - 1;
            mFirstVisible = (int)Math.Ceiling(mImageListView.ViewOffset.X / (float)mItemSizeWithMargin.Width) * mDisplayedRows;
            mLastVisible = (int)Math.Floor((mImageListView.ViewOffset.X + mItemAreaBounds.Width) / (float)mItemSizeWithMargin.Width) * mDisplayedRows - 1;
        }
        else
        {
            mFirstPartiallyVisible = (int)Math.Floor(mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mDisplayedCols;
            mLastPartiallyVisible = (int)Math.Ceiling((mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mDisplayedCols - 1;
            mFirstVisible = (int)Math.Ceiling(mImageListView.ViewOffset.Y / (float)mItemSizeWithMargin.Height) * mDisplayedCols;
            mLastVisible = (int)Math.Floor((mImageListView.ViewOffset.Y + mItemAreaBounds.Height) / (float)mItemSizeWithMargin.Height) * mDisplayedCols - 1;
        }

        // Bounds check
        if (mFirstPartiallyVisible < 0) mFirstPartiallyVisible = 0;
        if (mFirstPartiallyVisible > mImageListView.Items.Count - 1) mFirstPartiallyVisible = mImageListView.Items.Count - 1;
        if (mLastPartiallyVisible < 0) mLastPartiallyVisible = 0;
        if (mLastPartiallyVisible > mImageListView.Items.Count - 1) mLastPartiallyVisible = mImageListView.Items.Count - 1;
        if (mFirstVisible < 0) mFirstVisible = 0;
        if (mFirstVisible > mImageListView.Items.Count - 1) mFirstVisible = mImageListView.Items.Count - 1;
        if (mLastVisible < 0) mLastVisible = 0;
        if (mLastVisible > mImageListView.Items.Count - 1) mLastVisible = mImageListView.Items.Count - 1;

        // Cache visible items
        cachedVisibleItems.Clear();

        if (mFirstPartiallyVisible >= 0 &&
            mLastPartiallyVisible >= 0 &&
            mFirstPartiallyVisible <= mImageListView.Items.Count - 1 &&
            mLastPartiallyVisible <= mImageListView.Items.Count - 1)
        {
            for (int i = mFirstPartiallyVisible; i <= mLastPartiallyVisible; i++)
                cachedVisibleItems.Add(mImageListView.Items[i].Guid, false);
        }

        // Current item state processed
        mImageListView.Items.collectionModified = false;
    }
    
    #endregion
}
