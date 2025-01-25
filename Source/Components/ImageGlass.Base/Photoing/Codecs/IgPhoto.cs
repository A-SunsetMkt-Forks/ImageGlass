﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2024 DUONG DIEU PHAP
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
*/

namespace ImageGlass.Base.Photoing.Codecs;

/// <summary>
/// Initialize <see cref="IgPhoto"/> instance
/// </summary>
/// <param name="filename"></param>
public class IgPhoto(string filename) : IDisposable
{
    #region IDisposable Disposing

    public bool IsDisposed { get; private set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            Unload();
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public virtual void Dispose()
    {
        CancelLoading();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IgPhoto()
    {
        Dispose(false);
    }

    #endregion


    private CancellationTokenSource? _tokenSrc;


    #region Public properties

    /// <summary>
    /// Gets, sets original filename.
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Gets, sets working filename.
    /// </summary>
    public string Filename { get; set; } = filename;

    /// <summary>
    /// Gets file extension. E.g: <c>.png</c>.
    /// </summary>
    public string Extension => Path.GetExtension(Filename);

    /// <summary>
    /// Gets the error details
    /// </summary>
    public Exception? Error { get; private set; } = null;

    /// <summary>
    /// Gets the value indicates that image loading is done.
    /// </summary>
    public bool IsDone { get; private set; } = false;

    /// <summary>
    /// Gets, sets number of image frames.
    /// </summary>
    public int FrameCount { get; set; } = 0;

    /// <summary>
    /// Gets the image data.
    /// </summary>
    public IgImgData ImgData { get; internal set; } = new();

    /// <summary>
    /// Gets, sets image metadata
    /// </summary>
    public IgMetadata? Metadata { get; set; }

    #endregion


    #region Public functions


    /// <summary>
    /// Load the photo.
    /// </summary>
    /// <param name="options"></param>
    /// <exception cref="NullReferenceException"></exception>
    private async Task LoadImageAsync(CodecReadOptions? options = null)
    {
        // reset dispose status
        IsDisposed = false;

        // reset done status
        IsDone = false;

        // reset error
        Error = null;

        options ??= new();

        try
        {
            // load image data
            Metadata ??= PhotoCodec.LoadMetadata(Filename, options);
            FrameCount = Metadata?.FrameCount ?? 0;

            if (options.FirstFrameOnly == null)
            {
                options = options with
                {
                    FirstFrameOnly = FrameCount < 2,
                };
            }

            // cancel if requested
            if (_tokenSrc is not null && _tokenSrc.IsCancellationRequested)
            {
                _tokenSrc.Token.ThrowIfCancellationRequested();
            }

            // load image
            ImgData = await PhotoCodec.LoadAsync(Filename, options, null, _tokenSrc?.Token);

            // update metadata for JXR format
            if (Metadata.FileExtension == ".JXR")
            {
                Metadata.RenderedWidth = Metadata.OriginalWidth = (uint)(ImgData.Image?.Width ?? 0);
                Metadata.RenderedHeight = Metadata.OriginalHeight = (uint)(ImgData.Image?.Height ?? 0);
            }

            // cancel if requested
            if (_tokenSrc is not null && _tokenSrc.IsCancellationRequested)
            {
                _tokenSrc.Token.ThrowIfCancellationRequested();
            }

            // done loading
            IsDone = true;
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            Unload();
            Dispose();
        }
        catch (Exception ex)
        {
            // save the error
            Error = ex;

            // done loading
            IsDone = true;
        }
    }


    /// <summary>
    /// Read and load image into memory.
    /// </summary>
    /// <param name="options"></param>
    public async Task LoadAsync(
        CodecReadOptions? options = null,
        CancellationTokenSource? tokenSrc = null)
    {
        _tokenSrc = tokenSrc ?? new();

        await LoadImageAsync(options);
    }

    /// <summary>
    /// Unload the image and reset the relevant info
    /// </summary>
    public void Unload()
    {
        // reset info
        IsDone = false;
        Error = null;
        FrameCount = 0;

        // unload image
        ImgData?.Dispose();
    }


    /// <summary>
    /// Cancels image loading.
    /// </summary>
    public void CancelLoading()
    {
        try
        {
            _tokenSrc?.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    #endregion

}
