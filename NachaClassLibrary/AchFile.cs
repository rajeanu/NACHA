﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NachaClassLibrary
{
    public class AchFile
    {
        internal const int CharactersPerLine = 94;

        public virtual FileHeader Header { get; set; } = new FileHeader();

        public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

        public FileControl Control => new FileControl(this);
        
        public virtual string Generate()
        {
            // Data Specifications:

            // Alphameric and Alphabetic fields: left-justified and space filled

            // Numeric fields: right-justified, unsigned and zero filled

            // Characters used are restricted to: 0-9, A-Z, a-z, space, special characters

            // Field specific: require specific data in them or a requirement to be left blank

            // Upper case characters MUST be used for:

            //      Standard Entry Class (SEC) code field
            //      File ID modifier field
            //      Change code field and refused COR change field
            //      Return reason code fields (all types of returns)
            //      Company entry description fields containing any of these words: reversal, reclaim, nonsettled, autoenroll, redepcheck, no check, return fee, hcclaimpmt

            var output = new StringBuilder();

            // Sequence (1): File header record

            // Sequence (2): Company/Batch header record

            // Sequence (3): Entry detail, corporate entry detail record

            // Sequence (4): Addenda record

            // Sequence (5): Company/Batch control record

            // Sequence (6): File control record

            output.Append(Header);

            var batchNumber = 1;

            foreach (var batch in Batches)
            {
                batch.Header.BatchNumber = batchNumber;

                output.Append(batch);

                batchNumber++; // increment the batch number
            }

            output.Append(Control);

            // An ACH file must be "BLOCKED":

            // contain enough ACH records to form a complete "block" (10 records = 1 block = 940 characters)

            // All records within each ACH file are counted (headers, controls, entry details, addenda)

            // If the total number of records do not equal a complete block, "9 filler records" must be added to complete the block

            // A filler record is 94 characters of 9's

            var linesNeeded = 10 - GetNumberOfLines(this) % 10;

            var fillerRecord = GetFillerRecord();
            for (var i = 0; i < linesNeeded; i++)
            {
                output.AppendLine(fillerRecord);
            }
            //foreach (var i in Enumerable.Range(1, linesNeeded))
            //{
            //    output.AppendLine(fillerRecord);
            //}

            return output.ToString();
        }

        public static int GetNumberOfLines(AchFile file)
        {
            var lines = 1; // file header

            foreach (var batch in file.Batches)
            {
                lines += 2; // batch header + control
                lines += batch.Entries.Count;
            }

            lines += 1; // file control

            return lines;
        }

        public static string GetFillerRecord()
        {
            return new string('9', CharactersPerLine);
        }
    }

    public enum TransactionCode
    {
        CheckingCredit = 22,

        CheckingDebit = 27,

        CheckingCreditPrenote = 23,

        CheckingDebitPrenote = 28,

        SavingCredit = 32,

        SavingDebit = 37,

        SavingCreditPrenote = 33,

        SavingDebitPrenote = 38,
    }

    public enum ServiceClassCode
    {
        MixedDebitsAndCredits = 200,

        CreditsOnly = 220,

        DebitsOnly = 225,

        AutomatedAccountingAdvices = 280
    }

    /// <summary>
    /// Where did it come from and where is it going?
    /// </summary>
    public class FileHeader
    {
        /// <summary>
        /// THIS IS THE FIRST POSITION OF ALL RECORD FORMATS. THE CODE IS UNIQUE FOR EACH RECORD TYPE.
        /// THE FILE HEADER RECORD USED RECORD TYPE CODE 1.
        /// </summary>
        public readonly int RecordTypeCode = 1;

        /// <summary>
        /// PRIORITY CODES ARE NOT USED AT THIS TIME; THIS FIELD CONTAIN 01.
        /// </summary>
        public int PriorityCode = 1;

        /// <summary>
        /// ENTER YOUR PNC BANK TRANSIT/ROUTING NUMBER PRECEDED BY A BLANK SPACE I.E. B999999999.
        /// </summary>
        public string ImmediateDestination = string.Empty; // ACHBankRoutingNumber

        /// <summary>
        /// THIS FIELD IDENTIFIES THE ORGANIZATION OR COMPANY ORIGINATING THE
        /// FILE. THE FIELD BEGINS WITH A NUMBER, USUALLY '1' AND THE ORIGINATOR'S
        /// 9-DIGIT TAX ID WILL FOLLOW. IF THE FIELD CANNOT BE POPULATED WITH 10 DIGITS,
        /// A BLANK AND 9 DIGITS CAN BE USED.
        /// </summary>
        public string ImmediateOrigin = string.Empty;

        /// <summary>
        /// DATE WHEN THE ORIGINATOR CREATED THE FILE. THE DATE MUST BE IN "YYMMDD" FORMAT.
        /// </summary>
        public DateTime FileCreationDateTime = DateTime.UtcNow;

        // public String FileCreationTime = String.Empty;

        /// <summary>
        /// THIS PROVIDES A MEANS FOR AN ORIGINATOR TO DISTINGUISH BETWEEN MULTIPLE FILES CREATED ON THE SAME DATE. ONLY UPPERCASE, A-Z AND NUMBERS, 0-9 ARE PERMITTED.
        /// </summary>
        public string FileIdModifier = "A";

        /// <summary>
        /// THIS FIELD INDICATES THE NUMBER OF CHARACTERS CONTAINED IN EACH RECORD. THE VALUE 094 IS USED.
        /// </summary>
        public readonly int RecordSize = 94;

        /// <summary>
        /// THIS BLOCKING FACTOR DEFINES THE NUMBER OF PHYSICAL RECORDS WITHIN A FILE. THE VALUE 10 MUST BE USED.
        /// </summary>
        public readonly int BlockingFactor = 10;

        /// <summary>
        /// THIS FIELD MUST CONTAIN 1.
        /// </summary>
        public readonly int FormatCode = 1;

        /// <summary>
        /// The bank name that you're sending to, i.e. "PNC BANK"
        /// </summary>
        public string ImmediateDestinationName = string.Empty; // ACHFileBankName

        /// <summary>
        /// THIS FIELD IDENTIFIES THE ORIGINATOR OF THE FILE. THE NAME OF THE ORIGINATING COMPANY SHOULD BE USED.
        /// </summary>
        public string ImmediateOriginName = string.Empty;

        /// <summary>
        /// BLANKS FILL THIS FIELD.
        /// </summary>
        public readonly string ReferenceCode = string.Empty;

        public override string ToString()
        {
            var sb = new StringBuilder(AchFile.CharactersPerLine);

            sb.Append(RecordTypeCode);

            sb.Append(PriorityCode.ToString().TrimAndPadLeft(2, '0'));

            sb.Append(ImmediateDestination.TrimAndPadLeft(10));

            sb.Append(ImmediateOrigin.TrimAndPadRight(10));

            sb.Append(FileCreationDateTime.ToString("yyMMdd"));

            sb.Append(FileCreationDateTime.ToString("HHmm"));

            sb.Append(FileIdModifier.TrimAndPadRight(1));

            sb.Append(RecordSize.ToString().TrimAndPadLeft(3, '0'));

            sb.Append(BlockingFactor);

            sb.Append(FormatCode);

            sb.Append(ImmediateDestinationName.TrimAndPadRight(23));

            sb.Append(ImmediateOriginName.TrimAndPadRight(23));

            sb.Append(ReferenceCode.TrimAndPadRight(8));

            sb.AppendLine();

            return sb.ToString();
        }
    }

    /// <summary>
    /// Grand total of transactions and amounts
    /// </summary>
    public class FileControl
    {
        internal FileControl(AchFile file)
        {
            BatchCount = file.Batches.Count;

            EntryCount = file.Batches.Sum(batch => batch.Entries.Count);

            TotalCredits = file.Batches.Sum(batch => batch.Control.TotalCredits);
            NumberOfCredits = file.Batches.Sum(batch => batch.Control.NumberOfCredits);

            TotalDebits = file.Batches.Sum(batch => batch.Control.TotalDebits);
            NumberOfDebits = file.Batches.Sum(batch => batch.Control.NumberOfDebits);

            BlockCount = (int)Math.Ceiling(AchFile.GetNumberOfLines(file) / 10m);

            EntryHash = CalculateEntryHash(file);
        }

        /// <summary>
        /// THIS IS THE FIRST POSITION FOR ALL RECORD FORMATS. THE NUMBER
        /// IS UNIQUE FOR EACH RECORD TYPE. THE FILE CONTROL RECORD USES
        /// RECORD TYPE CODE 9.
        /// </summary>
        public readonly int RecordType = 9;

        /// <summary>
        /// VALUE MUST BE EQUAL TO THE NUMBER OF ‘8’ BATCH RECORDS IN FILE.
        /// </summary>
        public int BatchCount { get; internal set; }

        /// <summary>
        /// NUMBER OF PHYSICAL BLOCKS IN THE FILE, INCLUDING FILE HEADER AND FILE CONTROL RECORDS.
        /// </summary>
        public int BlockCount { get; internal set; }

        /// <summary>
        /// SUM OF ALL ‘6’ RECORDS AND ALSO '7' RECORDS, IF USED.
        /// </summary>
        public int EntryCount { get; internal set; }

        /// <summary>
        /// SUM OF ALL RECEIVING DEPOSITORY FINANCIAL INSTITUTION IDS IN EACH ‘6’ RECORD.
        /// IF SUM IS MORE THAN 10 POSITIONS, TRUNCATE LEFTMOST NUMBERS.
        /// </summary>
        public string EntryHash { get; internal set; }

        /// <summary>
        /// TOTAL OF ALL DEBIT AMOUNTS IN ‘8’ RECORDS, POSITIONS 21-32.
        /// </summary>
        public decimal TotalDebits { get; internal set; }

        /// <summary>
        /// COUNT OF ALL DEBITS IN ‘8’ RECORDS, POSITIONS 33-44.
        /// </summary>
        public int NumberOfDebits { get; internal set; }

        /// <summary>
        /// TOTAL OF ALL CREDIT AMOUNTS IN ‘8’ RECORDS, POSITIONS 33-44.
        /// </summary>
        public decimal TotalCredits { get; internal set; }

        /// <summary>
        /// COUNT OF ALL DEBITS IN ‘8’ RECORDS, POSITIONS 33-44.
        /// </summary>
        public int NumberOfCredits { get; internal set; }

        public readonly string Reserved = "      ";

        public override string ToString()
        {
            var sb = new StringBuilder(AchFile.CharactersPerLine);

            sb.Append(RecordType);

            sb.Append(BatchCount.ToString().TrimAndPadLeft(6, '0'));

            sb.Append(BlockCount.ToString().TrimAndPadLeft(6, '0'));

            sb.Append(EntryCount.ToString().TrimAndPadLeft(8, '0'));

            sb.Append(EntryHash.TrimAndPadLeft(10, '0'));

            sb.Append((TotalDebits * 100).ToString("0").TrimAndPadLeft(12, '0')); // 123.45 => 000000012345

            sb.Append((TotalCredits * 100).ToString("0").TrimAndPadLeft(12, '0')); // 123.45 => 000000012345

            sb.Append(Reserved.TrimAndPadRight(39));

            sb.AppendLine();

            return sb.ToString();
        }

        public static string CalculateEntryHash(AchFile file)
        {
            var sum = file.Batches.SelectMany(batch => batch.Entries)
                .Select(entry => entry.ReceivingDfiIdentification)
                .Select(routingNumber => routingNumber.Substring(0, 8))
                .Select(decimal.Parse)
                .Sum();

            //IF THE SUM OF THE RDFI TRANSIT ROUTING NUMBERS IS A
            //NUMBER GREATER THAN TEN DIGITS, REMOVE OR DROP THE
            //NUMBER OF DIGITS FROM THE LEFT SIDE OF THE NUMBER
            //UNTIL ONLY TEN DIGITS REMAIN. FOR EXAMPLE, IF THE SUM
            //OF THE TRANSIT ROUTING NUMBERS IS 234567898765,
            //REMOVE THE “23” FOR A HASH OF 4567898765.

            var last10 = sum % 10000000000;

            return last10.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class Batch
    {
        public virtual BatchHeader Header { get; set; } = new BatchHeader();

        public ICollection<EntryDetail> Entries { get; set; } = new List<EntryDetail>();

        public BatchControl Control => new BatchControl(this);

        // TODO Addendas? As of Now Odessa Product Has No Addendas record.
        
        public override string ToString()
        {
            var output = new StringBuilder();

            // This is a fix to ensure the batch header service code matches the calculated control

            Header.ServiceClass = Control.ServiceClass;

            output.Append(Header);

            foreach (var entry in Entries)
            {
                output.Append(entry);
            }

            output.Append(Control);

            return output.ToString();
        }
    }

    /// <summary>
    /// Who is it from and what is it?
    /// </summary>
    public class BatchHeader
    {
        /// <summary>
        /// THIS IS THE FIRST POSITION FOR ALL RECORD FORMATS. CODE
        /// IS UNIQUE FOR EACH RECORD TYPE.THE COMPANY/BATCH HEADER RECORD USED RECORD TYPE CODE 5.
        /// </summary>
        public readonly int RecordTypeCode = 5;

        /// <summary>
        /// THE SERVICE CLASS CODE DEFINES THE TYPE OF ENTRIES CONTAINED IN THE BATCH.
        /// </summary>
        public ServiceClassCode ServiceClass { get; set; } = ServiceClassCode.MixedDebitsAndCredits;

        /// <summary>
        /// THIS FIELD IDENTIFIES THE COMPANY THAT HAS THE RELATIONSHIP WITH
        /// THE RECEIVERS OF THE ACH TRANSACTIONS. THE NAME MUST MATCH THE
        /// "ACH EXHIBIT OR DOCUMENT B" FROM PNC BANK'S AGREEMENT. IN
        /// ACCORDANCE WITH FEDERAL REGULATION E, MOST RECEIVING FINANCIAL
        /// INSTITUTIONS WILL DISPLAY THIS FIELD ON THEIR CUSTOMER'S BANK STATEMENT.
        /// </summary>
        public string CompanyName = string.Empty; // control name?

        /// <summary>
        /// RFERENCE INFORMATION FOR USE BY THE ORIGINATOR.
        /// </summary>
        public string CompanyDiscreationaryData = string.Empty;

        /// <summary>
        /// THIS FIELD IDENTIFIES THE ORIGINATOR OF THE TRANSACTION VIA THE
        /// ORIGINATOR'S FEDERAL TAX ID (IRS EIN). THIS FIELD BEGINS WITH THE
        /// NUMBER 1, FOLLOWED BY THE COMPANY'S 9-DIGIT TAX ID (WITHOUT A
        /// HYPHEN.) THE ID MUST MATCH THE ID LISTED ON THE "ACH EXHIBIT OR
        /// DOCUMENT B" FROM THE BANK'S AGREEMENT.
        /// </summary>
        public string CompanyId = string.Empty; // ACH-EIN-Prefix + ControlEIN

        /// <summary>
        /// THIS FIELD DEFINES THE TYPE OF ACH ENTRIES CONTAINED IN THE BATCH.
        /// ENTER: PPD (PREARRANGED PAYMENTS AND DEPOSITS) FOR CONSUMER TRANSACTIONS DESTINED TO AN INDIVIDUAL or CCD
        /// (CASH CONCENTRATION OR DISBURSEMENT) FOR CORPORATE TRANSACTIONS.
        /// </summary>
        public readonly string StandardEntryClassCode = "PPD";

        /// <summary>
        /// THIS FIELD IS USED BY THE ORIGINATOR TO PROVIDE A DESCRIPTION
        /// OF THE TRANSACTION FOR THE RECEIVER.FOR EXAMPLE, PAYROLL OR
        /// DIVIDEND, ETC.IN ACCORDANCE WITH REGULATION E, MOST RECEIVING
        /// BANKS WILL DISPLAY THIS FIELD ON THEIR BANK STATEMENT.
        /// </summary>
        public string EntryDescription = "Rent";

        /// <summary>
        /// THIS FIELD IS USED BY THE ORIGINATOR TO PROVIDE A DESCRIPTIVE DATE
        /// FOR THE RECEIVER. THIS IS SOLELY FOR DESCRIPTIVE PURPOSES AND WILL
        /// NOT BE USED TO CALCULATE SETTLEMENT OR USED FOR POSTING PURPOSES.
        /// MANY RECEIVING FINANCIAL INSTITUTIONS WILL DISPLAY THIS FIELD ON
        /// THE CONSUMER'S BANK STATEMENT.
        /// </summary>
        public DateTime CompanyDescriptiveDate = DateTime.Today; // CheckDate

        /// <summary>
        /// THIS REPRESENTS THE DATE ON WHICH THE ORIGINATOR INTENDS A BATCH OF ENTRIES TO BE SETTLED.
        /// </summary>
        public DateTime EffectiveEntryDate = DateTime.Today;

        public readonly string SettlementDate = string.Empty;

        public readonly char OriginatorStatusCode = '1';

        /// <summary>
        /// ENTER THE FIRST 8 DIGITS OF YOUR BANK ABA OR TRANSIT ROUTING NUMBER.
        /// </summary>
        public string OriginatingDfiIdentification = string.Empty; // ACHBankRoutingNumber

        /// <summary>
        /// USED BY THE ORIGINATOR TO ASSIGN A NUMBER
        /// IN ASCENDING SEQUENCE TO EACH BATCH IN THE FILE.
        /// </summary>
        public int BatchNumber { get; internal set; } = 1;

        public override string ToString()
        {
            var sb = new StringBuilder(AchFile.CharactersPerLine);

            sb.Append(RecordTypeCode);

            sb.Append((int)ServiceClass);

            sb.Append(CompanyName.TrimAndPadRight(16));

            sb.Append(CompanyDiscreationaryData.TrimAndPadRight(20));

            sb.Append(CompanyId.TrimAndPadRight(10));

            sb.Append(StandardEntryClassCode.TrimAndPadRight(3));

            sb.Append(EntryDescription.TrimAndPadRight(10));

            sb.Append(CompanyDescriptiveDate.ToString("yyMMdd"));

            sb.Append(EffectiveEntryDate.ToString("yyMMdd"));

            sb.Append(SettlementDate.TrimAndPadLeft(3));

            sb.Append(OriginatorStatusCode);

            sb.Append(OriginatingDfiIdentification.TrimAndPadRight(8));

            sb.Append(BatchNumber.ToString().TrimAndPadLeft(7, '0'));

            sb.AppendLine();

            return sb.ToString();
        }
    }

    /// <summary>
    /// How many transactions and total amounts?
    /// </summary>
    public class BatchControl
    {
        public BatchControl(Batch batch)
        {
            ServiceClass = batch.Header.ServiceClass;
            CompanyId = batch.Header.CompanyId;
            BatchNumber = batch.Header.BatchNumber;
            OriginatingDfiId = batch.Header.OriginatingDfiIdentification;

            EntryCount = batch.Entries.Count;

            EntryHash = CalculateEntryHash(batch);

            TotalDebits = batch.Entries.Where(entry => !IsCredit(entry.TransactionCode)).Sum(entry => entry.Amount);
            NumberOfDebits = batch.Entries.Count(entry => !IsCredit(entry.TransactionCode));

            TotalCredits = batch.Entries.Where(entry => IsCredit(entry.TransactionCode)).Sum(entry => entry.Amount);
            NumberOfCredits = batch.Entries.Count(entry => IsCredit(entry.TransactionCode));

            if (NumberOfCredits > 0 && NumberOfDebits > 0)
            {
                ServiceClass = batch.Header.ServiceClass = ServiceClassCode.MixedDebitsAndCredits;
            }
            else if (NumberOfDebits > 0)
            {
                ServiceClass = batch.Header.ServiceClass = ServiceClassCode.DebitsOnly;
            }
            else if (NumberOfCredits > 0)
            {
                ServiceClass = batch.Header.ServiceClass = ServiceClassCode.CreditsOnly;
            }
        }

        internal static bool IsCredit(TransactionCode code)
        {
            switch (code)
            {
                case TransactionCode.CheckingCredit:
                case TransactionCode.CheckingCreditPrenote:
                case TransactionCode.SavingCredit:
                case TransactionCode.SavingCreditPrenote:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// THIS IS THE FIRST POSITION FOR ALL RECORD FORMATS. THE CODE IS
        /// UNIQUE FOR EACH RECORD TYPE. THE COMPANY/BATCH CONTROL RECORD
        /// USES RECORD TYPE CODE 8.
        /// </summary>
        public readonly int RecordType = 8;

        /// <summary>
        /// THE SERVICE CLASS CODE DEFINES THE TYPE OF 02-04 ENTRIES CONTAINED IN THE BATCH.
        /// </summary>
        public ServiceClassCode ServiceClass { get; internal set; } = ServiceClassCode.MixedDebitsAndCredits;

        public string CompanyId = string.Empty; // group name or ACHEINPrefix + EIN

        /// <summary>
        /// COUNT IS A TALLY OF EACH TYPE ‘6’ RECORD AND IF USED, ALSO EACH ADDENDA WITHIN THE BATCH.
        /// </summary>
        public int EntryCount { get; internal set; }

        /// <summary>
        /// FOR EACH ORIGINATED TRANSACTION, YOU HAVE
        /// GENERATED A TYPE ‘6’ OR ENTRY DETAIL RECORD. ON THE
        /// ENTRY DETAIL RECORD THERE IS A RECEIVING DEPOSITORY
        /// FINANANCIAL INSTITUTION (RDFI)IDENTIFICATION(TRANSIT
        /// ROUTING NUMBER) LOCATED IN POSITIONS 4 THROUGH 11.
        /// THE FIRST 8 DIGITS OF EACH RDFI’s TRANSIT ROUTING
        /// NUMBER IS TREATED AS A NUMBER.

        /// ALL TRANSIT ROUTING NUMBERS WITHIN THE BATCH ARE
        /// ADDED TOGETHER FOR THE ENTRY HASH ON THE TYPE '8',
        /// BATCH CONTROL RECORD.ALL TRANSIT ROUTING NUMBERS
        /// WITHIN EACH FILE ARE ADDED TOGETHER TO CALCULATE THE
        /// VALUE OF THE ENTRY HASH ON THE TYPE '9', FILE CONTROL
        /// RECORD. (NOTE: DO NOT INCLUDE THE CHECK DIGIT OF THE
        /// TRANSIT ROUTING NUMBER, POSITION 12, IN THIS
        /// CALCULATION.) THE ENTRY HASH CALCULATIION CHECK IS
        /// USED IN THE PNC BANK FILE EDITING PROCESS TO HELP
        /// ENSURE DATA INTEGRITY OF THE BATCH AND FILE
        /// GENERATED BY YOUR PROCESSING.
        /// </summary>
        public string EntryHash { get; internal set; }

        /// <summary>
        /// SUM TOTAL OF ALL DEBIT AMOUNTS WITHIN BATCH’S TYPE ‘6’ RECORD.
        /// </summary>
        public decimal TotalDebits { get; internal set; }

        /// <summary>
        /// COUNT OF ALL DEBITS WITHIN BATCH’S TYPE ‘6’ RECORD.
        /// </summary>
        public int NumberOfDebits { get; internal set; }

        /// <summary>
        /// SUM TOTAL OF ALL CREDIT AMOUNTS WITHIN BATCH’S TYPE ‘6’ RECORD
        /// </summary>
        public decimal TotalCredits { get; internal set; }

        /// <summary>
        /// COUNT OF ALL CREDITS WITHIN BATCH’S TYPE ‘6’ RECORD.
        /// </summary>
        public int NumberOfCredits { get; internal set; }

        /// <summary>
        /// TAX ID PREFIXED WITH A NUMERIC
        /// </summary>
        public string CompanyIdentification = string.Empty;

        public readonly string MessageAuthenticationCode = string.Empty;

        public readonly string Reserved = "      ";

        /// <summary>
        /// FIRST 8 DIGITS OF BANK ABA NUMBER
        /// </summary>
        public string OriginatingDfiId = string.Empty;

        /// <summary>
        /// NUMBER ASSIGNED IN ASCENDING SEQUENCE TO EACH BATCH WITHIN THE FILE
        /// </summary>
        public int BatchNumber { get; internal set; }

        public static string CalculateEntryHash(Batch batch)
        {
            var sum =
                batch
                .Entries
                .Select(entry => entry.ReceivingDfiIdentification)
                .Select(routingNumber => routingNumber.Substring(0, 8))
                .Select(decimal.Parse)
                .Sum();

            //IF THE SUM OF THE RDFI TRANSIT ROUTING NUMBERS IS A
            //NUMBER GREATER THAN TEN DIGITS, REMOVE OR DROP THE
            //NUMBER OF DIGITS FROM THE LEFT SIDE OF THE NUMBER
            //UNTIL ONLY TEN DIGITS REMAIN. FOR EXAMPLE, IF THE SUM
            //OF THE TRANSIT ROUTING NUMBERS IS 234567898765,
            //REMOVE THE “23” FOR A HASH OF 4567898765.

            var last10 = sum % 10000000000;

            return last10.ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(AchFile.CharactersPerLine);

            sb.Append(RecordType);

            sb.Append((int)ServiceClass);

            sb.Append(EntryCount.ToString().TrimAndPadLeft(6, '0'));

            sb.Append(EntryHash.TrimAndPadLeft(10, '0'));

            sb.Append((TotalDebits * 100).ToString("0").TrimAndPadLeft(12, '0'));

            sb.Append((TotalCredits * 100).ToString("0").TrimAndPadLeft(12, '0'));

            sb.Append(CompanyId.TrimAndPadRight(10));

            sb.Append(MessageAuthenticationCode.TrimAndPadRight(19));

            sb.Append(Reserved.TrimAndPadRight(6));

            sb.Append(OriginatingDfiId.TrimAndPadRight(8));

            sb.Append(BatchNumber.ToString().TrimAndPadLeft(7, '0'));

            sb.AppendLine();

            return sb.ToString();
        }
    }

    /// <summary>
    /// What is the RDFI, receiver, and amount?
    /// </summary>
    public class EntryDetail
    {
        public readonly int RecordTypeCode = 6;

        /// <summary>
        /// THE TRANSACTION CODE IDENTIFIES THE TYPE OF ENTRY.
        /// </summary>
        public TransactionCode TransactionCode = TransactionCode.CheckingCredit;

        /// <summary>
        /// FIRST 8 DIGITS OF THE RECEIVER’S BANK TRANSIT ROUTING NUMBER AT
        /// THE FINANCIAL INSTITUTION WHERE THE RECEIVER'S ACCOUNT IS MAINTAINED.
        /// </summary>
        public string ReceivingDfiIdentification = string.Empty;

        /// <summary>
        /// LAST DIGIT OF RECEIVER'S BANK TRANSIT ROUTING NUMBER.
        /// </summary>
        public char CheckDigit;

        /// <summary>
        /// THIS IS THE RECEIVER’S BANK ACCOUNT NUMBER. IF THE ACCOUNT NUMBER
        /// EXCEEDS 17 POSITIONS, ONLY USE THE LEFT MOST 17 CHARACTERS. ANY
        /// SPACES WITHIN THE ACCOUNT NUMBER SHOULD BE OMITTED WHEN PREPARING
        /// THE ENTRY. THIS FIELD MUST BE LEFT JUSTIFIED.
        /// </summary>
        public string DfiAccountNumber = string.Empty;

        /// <summary>
        /// The amount of the transaction in decimal format. Will be converted when writing the file.
        /// </summary>
        public decimal Amount = decimal.Zero;

        /// <summary>
        /// THIS IS AN IDENTIFYING NUMBER BY WHICH THE RECEIVER IS KNOWN TO
        /// THE ORIGINATOR. IT IS INCLUDED FOR FURTHER IDENTIFICATION AND
        /// DESCRIPTIVE PURPOSES.
        /// </summary>
        public string IndividualIdentificationNumber = string.Empty;

        /// <summary>
        /// THIS IS THE NAME IDENTIFYING THE RECEIVER OF THE TRANSACTION.
        /// </summary>
        public string IndividualName = string.Empty;

        /// <summary>
        /// THIS FIELD MUST BE LEFT BLANK.
        /// </summary>
        public readonly string DiscretionaryData = string.Empty;

        /// <summary>
        /// IF PPD OR CCD, ENTER 0 IN THIS FIELD TO INDICATE NO ADDENDA
        /// RECORD WILL FOLLOW. IF AN ADDENDA DOES FOLLOW THIS DETAIL RECORD,
        /// ENTER 1 TO INDICATE A '7' RECORD WILL FOLLOW.
        /// </summary>
        public readonly int AddendaRecordIndicator = 0;

        /// <summary>
        /// THE TRACE NUMBER IS A MEANS FOR THE
        /// ORIGINATOR TO IDENTIFY THE INDIVIDUAL ENTRIES.THE FIRST 8 POSITIONS
        /// OF THE FIELD SHOULD BE YOUR PNC BANK TRANSIT ROUTING NUMBER (WITHOUT THE
        /// CHECK DIGIT). THE REMAINDER OF THE FIELD MUST BE A UNIQUE NUMBER,
        /// ASSIGNED IN ASCENDING ORDER FOR EACH ENTRY.TRACE NUMBERS MAY BE
        /// DUPLICATED ACROSS DIFFERENT FILES.
        /// </summary>
        public string TraceNumber = string.Empty; // Routing # + company record count

        public override string ToString()
        {
            var sb = new StringBuilder(AchFile.CharactersPerLine);

            sb.Append(RecordTypeCode);

            sb.Append((int)TransactionCode);

            sb.Append(ReceivingDfiIdentification.TrimAndPadRight(8));

            sb.Append(CheckDigit);

            sb.Append(DfiAccountNumber.TrimAndPadRight(17));

            sb.Append((Amount * 100).ToString("0").TrimAndPadLeft(10, '0')); // 123.45 => 0000012345

            sb.Append(IndividualIdentificationNumber.TrimAndPadRight(15));

            sb.Append(IndividualName.TrimAndPadRight(22));

            sb.Append(DiscretionaryData.TrimAndPadRight(2));

            sb.Append(AddendaRecordIndicator.ToString().TrimAndPadLeft(1, '0'));

            sb.Append(TraceNumber.TrimAndPadLeft(15, '0'));

            sb.AppendLine();

            return sb.ToString();
        }
    }
}