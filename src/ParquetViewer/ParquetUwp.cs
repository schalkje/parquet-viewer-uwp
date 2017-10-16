﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parquet;
using Parquet.Data;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace ParquetViewer
{
   static class ParquetUwp
   {
      public const int MaxRows = 500;

      public static async Task<DataSet> OpenFromFilePickerAsync()
      {
         var picker = new FileOpenPicker
         {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
         };
         picker.FileTypeFilter.Add(".parquet");

         StorageFile file = await picker.PickSingleFileAsync();
         if (file == null) return null;

         using (IRandomAccessStreamWithContentType uwpStream = await file.OpenReadAsync())
         {
            DataSet ds = await Open(uwpStream);

            return ds;
         }
      }

      public static async Task<DataSet> OpenFromDragDropAsync(DragEventArgs e)
      {
         StorageFile storageFile = await GetFirstParquetFileAsync(e);
         if (storageFile == null) return null;

         using (IRandomAccessStreamWithContentType uwpStream = await storageFile.OpenReadAsync())
         {
            DataSet ds = await Open(uwpStream);

            return ds;
         }
      }

      public static async Task<StorageFile> GetFirstParquetFileAsync(DragEventArgs e)
      {
         if (e.DataView.Contains(StandardDataFormats.StorageItems))
         {
            IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();

            if (items.Count > 0)
            {
               var storageFile = items[0] as StorageFile;

               if (storageFile != null && Path.GetExtension(storageFile.Name).ToLower() == ".parquet")
               {
                  return storageFile;
               }
            }
         }

         return null;
      }

      public static async Task<DataSet> OpenFromFileAssociationAsync(NavigationEventArgs e)
      {
         var args = e.Parameter as Windows.ApplicationModel.Activation.IActivatedEventArgs;
         if (args != null)
         {
            if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
            {
               var fileArgs = args as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
               string strFilePath = fileArgs.Files[0].Path;
               var file = (StorageFile)fileArgs.Files[0];

               using (IRandomAccessStreamWithContentType uwpStream = await file.OpenReadAsync())
               {
                  DataSet ds = await Open(uwpStream);

                  return ds;
               }
            }
         }

         return null;
      }

      private static async Task<DataSet> Open(IRandomAccessStreamWithContentType uwpStream)
      {
         using (Stream stream = uwpStream.AsStreamForRead())
         {
            var readerOptions = new ReaderOptions()
            {
               Offset = 0,
               Count = MaxRows
            };

            var formatOptions = new ParquetOptions
            {
               TreatByteArrayAsString = true
            };

            try
            {
               return ParquetReader.Read(stream, formatOptions, readerOptions);
            }
            catch(Exception ex)
            {
               var dialog = new MessageDialog(ex.Message, "Cannot open file");
               await dialog.ShowAsync();
               return null;
            }
         }
      }
   }
}
