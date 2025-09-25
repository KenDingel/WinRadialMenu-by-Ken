using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace RadialMenu.Utilities
{
    /// <summary>
    /// Utility class for safe clipboard operations with retry logic.
    /// Handles the common OpenClipboard COM error (0x800401D0) that occurs 
    /// when another application temporarily has the clipboard locked.
    /// </summary>
    public static class ClipboardHelper
    {
        private const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;
        
        /// <summary>
        /// Safely sets text to the clipboard with minimal delay and smart retry logic.
        /// </summary>
        /// <param name="text">The text to set to the clipboard</param>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 2)</param>
        /// <param name="retryDelayMs">Delay between retry attempts in milliseconds (default: 50)</param>
        /// <returns>True if successful, false if all retries failed</returns>
        public static bool SetText(string text, int maxRetries = 2, int retryDelayMs = 50)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Fast path: try once without any retry logic overhead
            try
            {
                if (Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    Clipboard.SetText(text);
                    return true;
                }
                else
                {
                    // Invoke on UI thread if we're not already on it
                    var success = false;
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        try
                        {
                            Clipboard.SetText(text);
                            success = true;
                        }
                        catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                        {
                            // Will retry below
                            success = false;
                        }
                        catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                        {
                            // Will retry below
                            success = false;
                        }
                        catch
                        {
                            // Other errors - don't retry
                            success = false;
                        }
                    });
                    
                    if (success) return true;
                }
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
            {
                // Will retry below
            }
            catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
            {
                // Will retry below
            }
            catch (Exception)
            {
                // Other exceptions - don't retry, fail fast
                return false;
            }

            // Retry path: only for specific clipboard lock errors
            for (int attempt = 1; attempt < maxRetries; attempt++)
            {
                Thread.Sleep(retryDelayMs);
                
                try
                {
                    if (Application.Current?.Dispatcher?.CheckAccess() == true)
                    {
                        Clipboard.SetText(text);
                        return true;
                    }
                    else
                    {
                        var success = false;
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            try
                            {
                                Clipboard.SetText(text);
                                success = true;
                            }
                            catch
                            {
                                success = false;
                            }
                        });
                        return success;
                    }
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                {
                    // Continue retrying
                    continue;
                }
                catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                {
                    // Continue retrying
                    continue;
                }
                catch (Exception)
                {
                    // Other exceptions - stop retrying
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Safely gets text from the clipboard with minimal delay and smart retry logic.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts (default: 2)</param>
        /// <param name="retryDelayMs">Delay between retry attempts in milliseconds (default: 50)</param>
        /// <returns>The clipboard text, or null if failed</returns>
        public static string? GetText(int maxRetries = 2, int retryDelayMs = 50)
        {
            // Fast path: try once without any retry logic overhead
            try
            {
                if (Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    return Clipboard.ContainsText() ? Clipboard.GetText() : null;
                }
                else
                {
                    string? result = null;
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        try
                        {
                            result = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                        }
                        catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                        {
                            // Will retry below
                            result = null;
                        }
                        catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                        {
                            // Will retry below  
                            result = null;
                        }
                        catch
                        {
                            // Other errors - don't retry
                            result = null;
                        }
                    });
                    
                    if (result != null) return result;
                }
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
            {
                // Will retry below
            }
            catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
            {
                // Will retry below
            }
            catch (Exception)
            {
                // Other exceptions - don't retry, fail fast
                return null;
            }

            // Retry path: only for specific clipboard lock errors
            for (int attempt = 1; attempt < maxRetries; attempt++)
            {
                Thread.Sleep(retryDelayMs);
                
                try
                {
                    if (Application.Current?.Dispatcher?.CheckAccess() == true)
                    {
                        return Clipboard.ContainsText() ? Clipboard.GetText() : null;
                    }
                    else
                    {
                        string? result = null;
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            try
                            {
                                result = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                            }
                            catch
                            {
                                result = null;
                            }
                        });
                        return result;
                    }
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                {
                    // Continue retrying
                    continue;
                }
                catch (ExternalException ex) when (ex.HResult == unchecked((int)CLIPBRD_E_CANT_OPEN))
                {
                    // Continue retrying
                    continue;
                }
                catch (Exception)
                {
                    // Other exceptions - stop retrying
                    return null;
                }
            }
            
            return null;
        }
    }
}