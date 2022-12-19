using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;

namespace Project_ICT
{
    public partial class MainWindow : Window
    {
        SerialPort _serialPort = new SerialPort();
        int sliderValue;
        int sliderSpeedValue;
        int rainbowOffset;
        int red, green, blue;

        CancellationTokenSource source = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();

            PortName.Items.Add("None");
            foreach (string s in SerialPort.GetPortNames())
                PortName.Items.Add(s);

            sldr.IsEnabled = false;
            sldrSpeed.IsEnabled = false;
            btnOn.IsEnabled = false;
            btnOff.IsEnabled = false;
            btnSolid.IsEnabled = false;
            btnRainbow.IsEnabled = false;
            btnSwipe.IsEnabled = false;
            clrPckr.IsEnabled = false;
        }

        public async Task RainbowAsync(int speed, int offset)
        {
            if (source == null || source.IsCancellationRequested)
            {
                source = new CancellationTokenSource();
            }
            Task taskRainbow = Task.Run(() =>
            {
                while (!source.IsCancellationRequested)
                {
                    SendData(Rainbow(speed, offset));
                }
            },
            source.Token);
        }
 

        public async Task SwipeAsync(byte[] rgb, int speed)
        {
            if (source == null || source.IsCancellationRequested)
            {
                source = new CancellationTokenSource();
            }
            Task taskSwipe = Task.Run(async () =>
            {
                byte[] ledByte = new byte[81];
                byte[] color = new byte[3];
                while (!source.IsCancellationRequested)
                {
                    int leds = sliderValue;
                    for (int k = 0; k < 2; k++)
                    {
                        switch (k)
                        {
                            case 0:
                                color = rgb;
                                break;
                            case 1:
                                color = new byte[3] { 0, 0, 0 };
                                break;
                        }
                        for (int u = 0; u < leds; u++)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                ledByte[u * 3 + i] = color[i];
                            }
                            if (_serialPort.IsOpen)
                                _serialPort.Write(ledByte, 0, 81);
                            await Task.Delay(speed, source.Token);
                        }
                    }
                }
            },
            source.Token);
        }

        public byte[] Rainbow(int speed, int offset)
        {
            if (red > offset && blue == offset)
            {
                red -= speed;
                green += speed;
            }
            if (green > offset && red == offset)
            {
                green -= speed;
                blue += speed;
            }
            if (blue > offset && green == offset)
            {
                blue -= speed;
                red += speed;
            }
            byte _Red = Convert.ToByte(red);
            byte _Green = Convert.ToByte(green);
            byte _Blue = Convert.ToByte(blue);

            byte[] temp = new byte[3] { _Red, _Green, _Blue};

            return temp;
        }

       

        private void SendData(byte[] rgb)
        {
            byte[] ledByte = new Byte[81];
            int j = -1;

            for (int u = 0; u < sliderValue * 3;)
            {
                for (int i = 0; i < rgb.Length; i++)
                {
                    j++;
                    u++;

                    ledByte[j] = rgb[i];
                }
            }
            if (_serialPort.IsOpen)
                _serialPort.Write(ledByte, 0, 81);
        }

        private static String RGBConverter(System.Windows.Media.Color c)
        {
            String rtn = String.Empty;
            try
            {
                rtn = c.R.ToString() + "," + c.G.ToString() + "," + c.B.ToString();
            }
            catch (Exception ex)
            {
                
            }

            return rtn;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sliderValue = Convert.ToInt16(sldr.Value);
        }

        private void sldrSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sliderSpeedValue = Convert.ToInt16(sldrSpeed.Value);
            rainbowOffset = Convert.ToInt16(255-(255/sliderSpeedValue)*sliderSpeedValue);
        }

        private void PortName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();

                if (PortName.SelectedItem.ToString() != "None")
                {
                    _serialPort.PortName = PortName.SelectedItem.ToString();
                    _serialPort.Open();

                    sldr.IsEnabled = true;
                    sldrSpeed.IsEnabled = true;
                    btnOn.IsEnabled = true;
                    btnOff.IsEnabled = true;
                    btnSolid.IsEnabled = true;
                    btnRainbow.IsEnabled = true;
                   ;
                    btnSwipe.IsEnabled = true;
                    clrPckr.IsEnabled = true;
                }
                else
                {
                    sldr.IsEnabled = false;
                    sldrSpeed.IsEnabled= false;
                    btnOn.IsEnabled = false;
                    btnOff.IsEnabled = false;
                    btnSolid.IsEnabled = false;
                    btnRainbow.IsEnabled = false;
                    
                    btnSwipe.IsEnabled = false;
                    clrPckr.IsEnabled = false;
                }
            }
        }

        private void btnOn_Click(object sender, RoutedEventArgs e)
        {
            effectsOff();

            if (btnSolid.IsChecked == true)
                SendData(RGBConverter(clrPckr.Color).Split(',').Select(byte.Parse).ToArray());

            if (btnRainbow.IsChecked == true)
            {
                red = 255;
                green = rainbowOffset;
                blue = rainbowOffset;
                RainbowAsync(sliderSpeedValue, rainbowOffset);
            }

           


            if (btnSwipe.IsChecked == true)
                SwipeAsync(RGBConverter(clrPckr.Color).Split(',').Select(byte.Parse).ToArray(), (10 - sliderSpeedValue) * 10);

            if(btnSolid.IsChecked == false && btnRainbow.IsChecked == false && btnSwipe.IsChecked == false) 
                MessageBox.Show("selecteer eerst een effect");
        }

        private void btnOff_Click(object sender, RoutedEventArgs e)
        {
            effectsOff();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            effectsOff();
        }

        private void effectsOff()
        {
            if (source != null)
                source.Cancel();

            Byte[] ledByteOff = new byte[81];
            if (_serialPort.IsOpen)
                _serialPort.Write(ledByteOff, 0, 81);
        }
    }
}
