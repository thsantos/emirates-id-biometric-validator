using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmiratesId.AE;
using EmiratesId.AE.Exceptions;
using EmiratesId.AE.ReadersMgt;
using EmiratesId.AE.PublicData;using EmiratesId.AE.Utils;
using EmiratesId.AE.Biometrics;

namespace PatientValidation
{
    class Program
    {
        static PCSCReader reader;
        static ReaderManagement readerMgr;

        static PCSCReader ListReaders()
        {
            readerMgr.DiscoverReaders();
            var readers = readerMgr.Readers;
            int option;

            if (readers.Length == 1)
                return readers[0];

            //Return as default reader if only one is found.
            List:
            Console.Clear();
            Console.WriteLine("Available Readers:\n==============================================");
            for (int i = 0; i <= readers.Length - 1; i++)
            {
                Console.WriteLine("{0}) {1}", (i + 1), readers[i]);
            }

            Console.Write("Please enter the reader number:");
            if (!Int32.TryParse(Console.ReadLine(), out option))
            {
                goto List;
            }
            else
            {
                if (option < 1 || option > readers.Length)
                    goto List;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\"{0}\" selected.", readers[option - 1].ReaderName);
            Console.ResetColor();

            return readers[option - 1];
        }

        static PCSCReader GetReaderByName(string name)
        {
            readerMgr.DiscoverReaders();
            return readerMgr.SelectReaderByName(name);
        }

        static CardHolderPublicData ReadCard()
        {
            System.Threading.Thread.Sleep(1000);
            Console.Clear();

            bool isCardConnected = reader.IsConnected();
            if(!isCardConnected)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Please insert EID Card.");
                Console.ResetColor();

                return null;
            }

            try
            {
                PublicDataFacade publicDataFacade = reader.GetPublicDataFacade();

                bool isGenuine = publicDataFacade.IsCardGenuine();
                if(!isGenuine)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The inserted card is not genuine.");
                    Console.ResetColor();

                    return null;
                }


                CardHolderPublicData output = publicDataFacade.ReadPublicData(true, false, true, false, false);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Welcome {0}", EmiratesId.AE.Utils.Utils.ByteArrayToUTF8String(output.FullName));
                Console.ResetColor();

                return output;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Public Data error: {0}", ex.Message);
                Console.ResetColor();

                return null;
            }
        }

        static bool ReadBiometric()
        {
            //Give some time in order to Load the whole configuration
            System.Threading.Thread.Sleep(1000);


            BiometricFacade biometricFacade = reader.GetBiometricFacade();
            try
            {
                biometricFacade.ReadBiometricInfoTemplates();
                BIT firstBit = biometricFacade.FirstBit;
                BIT secondBit = biometricFacade.SecondBit;
                int refDataQualifier = firstBit.ReferenceDataQualifier;

                Console.WriteLine("Please place your \"{0}\" over the biometric sensor:", GetFingerIndexType(firstBit.FingerIndex).Name);

                try
                {
                    SensorDevice sensor = new SagemSensorDevice();
                    // SensorDevice sensor = new DermalogSensorDevice
                    FingerIndexType fingerIndex = FingerIndexType.LEFT_INDEX_TYPE;
                    FTP_Image image = sensor.CaptureImage(firstBit.FingerIndex);
                    BiometricFacade bio = reader.GetBiometricFacade();
                    FTP_Template template = bio.ConvertImage(image,FormatType.ISO_19794_CS.Format);

                    if (image == null || image.Pixels == null)
                        return false;
                    bool result = biometricFacade.MatchOnCard(firstBit, template);
                    //bool result = biometricFacade.MatchOffCard(firstBit, template);

                    //int result = biometricFacade.ReadFingerprints();

                    Console.WriteLine("Is Valid? {0}",result);
                }
                catch (MiddlewareException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Sensor Error: {0}", ex.Message);
                    Console.ResetColor();
                }




            }
            catch (MiddlewareException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Biometric error: {0}",ex.Message);
                Console.ResetColor();
            }
            return false;
        }

        static Model.FingersTypeView GetFingerIndexType(int id)
        {
            List<Model.FingersTypeView> fingerTypes = new List<Model.FingersTypeView>
            {
                new Model.FingersTypeView{ Id = FingerIndexType.RIGHT_THUMB_TYPE.Index, Type = FingerIndexType.RIGHT_THUMB_TYPE, Name="Right Hand Thumb" },
                new Model.FingersTypeView{ Id = FingerIndexType.RIGHT_INDEX_TYPE.Index, Type = FingerIndexType.RIGHT_INDEX_TYPE, Name="Right Hand Index Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.RIGHT_MIDDLE_TYPE.Index, Type = FingerIndexType.RIGHT_MIDDLE_TYPE, Name="Right Middle Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.RIGHT_RING_TYPE.Index, Type = FingerIndexType.RIGHT_RING_TYPE, Name="Right Hand Ring Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.RIGHT_LITTLE_TYPE.Index, Type = FingerIndexType.RIGHT_LITTLE_TYPE, Name="Right Hand Little Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.LEFT_THUMB_TYPE.Index, Type = FingerIndexType.LEFT_THUMB_TYPE, Name="Left Hand Thumb" },
                new Model.FingersTypeView{ Id = FingerIndexType.LEFT_INDEX_TYPE.Index, Type = FingerIndexType.LEFT_INDEX_TYPE, Name="Left Hand Index Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.LEFT_MIDDLE_TYPE.Index, Type = FingerIndexType.LEFT_MIDDLE_TYPE, Name="Left Middle Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.LEFT_RING_TYPE.Index, Type = FingerIndexType.LEFT_RING_TYPE, Name="Left Hand Ring Finger" },
                new Model.FingersTypeView{ Id = FingerIndexType.LEFT_LITTLE_TYPE.Index, Type = FingerIndexType.LEFT_LITTLE_TYPE, Name="Left Hand Little Finger" },
            };

            return fingerTypes.Find(f => f.Id == id);
        }

        static void Main(string[] args)
        {
            try
            {
                readerMgr = new ReaderManagement();
                readerMgr.EstablishContext();

                //List all the available readers
                SelectReader:
                reader = ListReaders();

                ////Read Card
                CardHolderPublicData card = ReadCard();
                if (card == null)
                    goto SelectReader;

                ////Read Biometric
                ReadBiometric();

                readerMgr.CloseContext();



                Console.Read();
            }
            catch (MiddlewareException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.Read();
            }

        }
    }
}
