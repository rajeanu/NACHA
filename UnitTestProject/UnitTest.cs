using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NachaClassLibrary;
namespace UnitTestProject
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod()
        {
        }

        [TestMethod]
        public void CreateFullNachaFileMethod()
        {
            var achNachaFile = new AchFile();
            var listOfBatch = new List<Batch>();
            achNachaFile.Header = new FileHeader()
            {
                 FileCreationDateTime = DateTime.Now,
                 FileIdModifier = "A",
                 ImmediateDestination= "1234567890",
                 ImmediateDestinationName= "ACHDesination",
                 ImmediateOrigin= "123456789",
                 ImmediateOriginName= "LessorCapitalLease",
                 PriorityCode=1
            };

            foreach (var CreateBatch in Enumerable.Range(1, 4))
            {
                var batch = new Batch();
                batch.Header=new BatchHeader()
                 {
                     ServiceClass = ServiceClassCode.DebitsOnly,
                    OriginatingDfiIdentification = "",
                    CompanyDescriptiveDate = new DateTime(), //settlementdate
                    CompanyDiscreationaryData = "LEASECHG-",
                    CompanyId = "TAXID1123",
                    CompanyName = "ODESSATECH",
                    EffectiveEntryDate = new DateTime(),
                    EntryDescription = "ACH AUTO DEDUCT"
                };
                var listOfEntries = new List<EntryDetail>();
                foreach (var CreateEntries in Enumerable.Range(1, 2))
                {
                    var entriesRecord = new EntryDetail()
                    {
                        Amount = 100.00M,
                        CheckDigit = '1',
                        DfiAccountNumber="11231231233567890",
                        IndividualIdentificationNumber="12321312",
                        IndividualName="Deep",
                        ReceivingDfiIdentification="12312345",
                        TraceNumber="000001",
                        TransactionCode=TransactionCode.CheckingDebit
                    };
                    listOfEntries.Add(entriesRecord);
                }

                batch.Entries = listOfEntries;
               // batch.Control = BatchControl(batch); no need as auto generate it
                listOfBatch.Add(batch);
            }
            achNachaFile.Batches = listOfBatch;
            var resultString=achNachaFile.Generate();
        }
    }
}
