﻿using System;
using GHIElectronics.TinyCLR.Devices.Spi;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Bauland.Others
{
    /// <summary>
    /// Wrapper to manage LedStrip based on APA102
    /// </summary>
    public class LedStrip
    {
        /// <summary>
        /// Type of order of colors for LED
        /// </summary>
        public enum ColorOrder
        {
            /// <summary>
            /// First Red, Green then Blue
            /// </summary>
            Rgb,

            /// <summary>
            /// First Blue, Green then Red
            /// </summary>
            Bgr
        };

        private readonly SpiDevice _spi;
        private readonly byte[] _dummy;
        private readonly byte[] _data;
        private readonly Led[] _leds;

        /// <summary>
        /// Size of strip in led number
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Constructor of LedStrip
        /// </summary>
        /// <param name="size">Size of strip in led number</param>
        /// <param name="spiBus">Id of spi bus</param>
        /// <param name="chipSelect">ChipSelect pin. May be a dummy pin (as not connected) as it don't be used</param>
        /// <param name="order">Order of colors</param>
        public LedStrip(int size, string spiBus, int chipSelect, ColorOrder order)
        {
            if (size < 1) throw new ArgumentOutOfRangeException(nameof(size), "must be greater than 0");
            Size = size;
            _leds = new Led[size];
            for (int i = 0; i < size; i++)
                _leds[i] = new Led(order);
            _dummy = new byte[4 + 4 * size + size / 8 / 2];
            _data = new byte[4 + 4 * size + size / 8 / 2];
            SpiConnectionSettings settings = new SpiConnectionSettings()
            {
                ChipSelectLine = chipSelect,
                DataBitLength = 8,
                ClockFrequency = 10 * 1000 * 1000
            };
            _spi = SpiController.FromName(spiBus).GetDevice(settings);
            PrepareStart();
            PrepareEnd();
        }

        /// <summary>
        /// Set all LEDs to black, then Show
        /// </summary>
        public void Clear()
        {
            SetAll(0xe0, 0, 0, 0);
            Show();
        }

        /// <summary>
        /// Set color of one LED in memory. No effect on LedStrip until Show() is call.
        /// </summary>
        /// <param name="index">Index of LED, must be between 0 and Size - 1</param>
        /// <param name="brightness">Brightness of LED</param>
        /// <param name="red">Value of red for LED color</param>
        /// <param name="green">Value of green for LED color</param>
        /// <param name="blue">Value of blue for LED color</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetPixel(int index, byte brightness, byte red, byte green, byte blue)
        {
            if (index < 0 || index >= Size) throw new ArgumentOutOfRangeException(nameof(index), "n must be between 0 and Size - 1.");
            var led = _leds[index];
            led.Red = red;
            led.Green = green;
            led.Blue = blue;
            led.Brightness = brightness;
        }

        /// <summary>
        /// Set color of all LEDs in memory. No effect on LedStrip until Show() is call.
        /// </summary>
        /// <param name="brightness">Brightness for all LEDs</param>
        /// <param name="red">Value of red for all LEDs color</param>
        /// <param name="green">Value of green for all LEDs color</param>
        /// <param name="blue">Value of blue for all LEDs color</param>
        public void SetAll(byte brightness, byte red, byte green, byte blue)
        {
            for (int i = 0; i < Size; i++)
            {
                var led = _leds[i];
                led.Brightness = brightness;
                led.Red = red;
                led.Blue = blue;
                led.Green = green;
            }
        }

        /// <summary>
        /// Render memory on LedStrip
        /// </summary>
        public void Show()
        {
            for (int i = 0; i < Size; i++)
            {
                _leds[i].GetData().CopyTo(_data, 4 + i * 4);
            }
            _spi.TransferFullDuplex(_data, _dummy);
        }

        private void PrepareEnd()
        {
            for (int i = 4 + 4 * Size; i < _data.Length; i++)
            {
                _data[i] = 0xff;
            }
        }

        private void PrepareStart()
        {
            for (int i = 0; i < 4; i++)
                _data[i] = 0;
        }
    }
}
