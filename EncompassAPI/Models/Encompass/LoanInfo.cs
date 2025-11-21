namespace EncompassAPI.Models.Encompass.LoanInfo
{
    public class LoanInfo
    {
        public string id { get; set; }
        public Additionalrequests additionalRequests { get; set; }
        public Application[] applications { get; set; }
        public Atrqmcommon atrqmCommon { get; set; }
        public float baseLoanAmount { get; set; }
        public int bltv { get; set; }
        public string borrowerCount { get; set; }
        public float borrowerPaidClosingCostsAmount { get; set; }
        public float borrowerPaidFhavaClosingCostsAmount { get; set; }
        public int borrowerPairCount { get; set; }
        public float borrowerRequestedLoanAmount { get; set; }
        public float cashFromToBorrowerAmount { get; set; }
        public Closingcost closingCost { get; set; }
        public Closingdocument closingDocument { get; set; }
        public float combinedLtv { get; set; }
        public Commitmentterms commitmentTerms { get; set; }
        public string conformingJumbo { get; set; }
        public Constructionmanagement constructionManagement { get; set; }
        public string consumerConnectSiteId { get; set; }
        public Contact[] contacts { get; set; }
        public Correspondent correspondent { get; set; }
        public int currentApplicationIndex { get; set; }
        public Customfield[] customFields { get; set; }
        public Custommodelfields customModelFields { get; set; }
        public Disclosurenotices disclosureNotices { get; set; }
        public string docEngine { get; set; }
        public float downPaymentPercent { get; set; }
        public Downpayment downPayment { get; set; }
        public Locompensation loCompensation { get; set; }
        public Emdocument emDocument { get; set; }
        public Emdocumentinvestor emDocumentInvestor { get; set; }
        public Emdocumentlender emDocumentLender { get; set; }
        public string encompassVersion { get; set; }
        public bool enforceCountyLoanLimit { get; set; }
        public string emXmlVersionId { get; set; }
        public float estimatedPrepaidItemsAmount { get; set; }
        public float existingLiensAndDrawUsed { get; set; }
        public Fanniemae fannieMae { get; set; }
        public Fee[] fees { get; set; }
        public Fhavaloan fhaVaLoan { get; set; }
        public float firstAdjustmentMinimum { get; set; }
        public float firstSubordinateLienAmount { get; set; }
        public Freddiemac freddieMac { get; set; }
        public Funding funding { get; set; }
        public Gfe gfe { get; set; }
        public float hcltvHtltv { get; set; }
        public Hmda hmda { get; set; }
        public Hud1es hud1Es { get; set; }
        public Hudloandata hudLoanData { get; set; }
        public float initialInterestRate { get; set; }
        public string initialInterestRateUi { get; set; }
        public Interimservicing interimServicing { get; set; }
        public string interviewerEmail { get; set; }
        public string interviewersId { get; set; }
        public string interviewerName { get; set; }
        public int loanAmortizationTermMonths { get; set; }
        public string loanAmortizationType { get; set; }
        public DateTime loanCreationDate { get; set; }
        public bool loanImportStatusIndicator { get; set; }
        public string loanNumber { get; set; }
        public Loanproductdata loanProductData { get; set; }
        public string loanProgramName { get; set; }
        public string loanSource { get; set; }
        public Loansubmission loanSubmission { get; set; }
        public float ltv { get; set; }
        public float ltvPropertyValue { get; set; }
        public string maturityDate { get; set; }
        public Mcaw mcaw { get; set; }
        public string milestoneApprovedDueDate { get; set; }
        public string milestoneCompletedDueDate { get; set; }
        public DateTime milestoneCurrentDate { get; set; }
        public string milestoneCurrentName { get; set; }
        public string milestoneNextName { get; set; }
        public string milestoneFileStartedDate { get; set; }
        public string milestoneFundedDueDate { get; set; }
        public Milestone[] milestones { get; set; }
        public string milestoneStage { get; set; }
        public string milestoneSubmittedDueDate { get; set; }
        public Miscellaneous miscellaneous { get; set; }
        public float monthlyPiPaymentAmountForLe1andCd1 { get; set; }
        public string mortgageType { get; set; }
        public Nettangiblebenefit netTangibleBenefit { get; set; }
        public string nmlsLoanOriginatorId { get; set; }
        public string occupancyType { get; set; }
        public string openingDocsLoanProgramType { get; set; }
        public string openingDocsPlanDescription { get; set; }
        public string openingDocsPlanId { get; set; }
        public string openingDocsProgramCode { get; set; }
        public string openingDocsStackingOrder { get; set; }
        public string originationDate { get; set; }
        public string originatorFirstName { get; set; }
        public string originatorLastName { get; set; }
        public bool paymentScheduleCalcRequiredIndicator { get; set; }
        public bool pmiIndicator { get; set; }
        public Prequalification prequalification { get; set; }
        public float principalAndInterestMonthlyPaymentAmount { get; set; }
        public string print2003Application { get; set; }
        public Privacypolicy privacyPolicy { get; set; }
        public Profitmanagement profitManagement { get; set; }
        public Property1 property { get; set; }
        public int propertyAppraisedValueAmount { get; set; }
        public int propertyEstimatedValueAmount { get; set; }
        public float proposedFirstMortgageAmount { get; set; }
        public float proposedOtherAmount { get; set; }
        public float purchasePriceAmount { get; set; }
        public Ratelock rateLock { get; set; }
        public Regulationz regulationZ { get; set; }
        public float requestedInterestRatePercent { get; set; }
        public string requestedInterestRatePercentUi { get; set; }
        public Section32 section32 { get; set; }
        public Selectedhomecounselingprovider selectedHomeCounselingProvider { get; set; }
        public Servicingdisclosure servicingDisclosure { get; set; }
        public Shipping shipping { get; set; }
        public Statedisclosure stateDisclosure { get; set; }
        public Statementcreditdenial statementCreditDenial { get; set; }
        public string systemIdGuid { get; set; }
        public string titleHolderName1 { get; set; }
        public string titleHolderName2 { get; set; }
        public float tltv { get; set; }
        public float totalClosingCostsAmount { get; set; }
        public float totalFeesCostAmount { get; set; }
        public float totalFeesCreditAmount { get; set; }
        public float totalWireTransferAmount { get; set; }
        public Tpo tpo { get; set; }
        public Tql tql { get; set; }
        public Trustaccount trustAccount { get; set; }
        public Tsum tsum { get; set; }
        public Uldd uldd { get; set; }
        public Underwritersummary underwriterSummary { get; set; }
        public bool underwritingEscrowIndicator { get; set; }
        public string urlaLoanIdentifier { get; set; }
        public Usda usda { get; set; }
        public bool use2018DiIndicator { get; set; }
        public string useNew2015FormsIndicator { get; set; }
        public Valoandata vaLoanData { get; set; }
        public float valuationUsed { get; set; }
        public DateTime serverDateTimeDdmApplied { get; set; }
        public float proposedHousingExpenseTotal { get; set; }
        public Collateraltracking collateralTracking { get; set; }
        public string titleWillBeFullName { get; set; }
        public float borrEstimatedClosingCostsAmount { get; set; }
        public float urlaTotalMortgageLoansAmount { get; set; }
        public bool printUliAndLoanNoUrla { get; set; }
        public int expectedBorrowerCount { get; set; }
        public bool useEnhancedConditionIndicator { get; set; }
        public bool isLoanAmountRounding { get; set; }
        public Aiq aiq { get; set; }
        public Eclose eClose { get; set; }
        public string armIndexPrecision { get; set; }
        public string changeCircumstanceReadyForDisclosureDate { get; set; }
        public bool archived { get; set; }
    }

    public class Additionalrequests
    {
    }

    public class Atrqmcommon
    {
        public string generalAtrStatusOverall { get; set; }
        public string generalAtrStatusDti { get; set; }
        public string generalAtrStatusResidualIncome { get; set; }
        public string generalAtrStatusCoveredLoan { get; set; }
        public string generalAtrStatusSimultaneousLoan { get; set; }
        public string generalAtrStatusMtgRelatedObligations { get; set; }
        public string generalAtrStatusEmployment { get; set; }
        public string generalAtrStatusIncome { get; set; }
        public string generalAtrStatusAssets { get; set; }
        public string generalAtrStatusDebtObligations { get; set; }
        public string generalAtrStatusAlimony { get; set; }
        public string generalAtrStatusChildSupport { get; set; }
        public string generalAtrStatusCreditHistory { get; set; }
        public string standardQmStatusOverall { get; set; }
        public string standardQmStatusLoanTerm { get; set; }
        public string standardQmStatusInterestOnly { get; set; }
        public string standardQmStatusNegativeAmortization { get; set; }
        public string standardQmStatusBalloonPayment { get; set; }
        public string standardQmStatusPrepaymentPenalty { get; set; }
        public string standardQmStatusPointsFeesLimit { get; set; }
        public string standardQmStatusDti { get; set; }
        public string standardQmStatusResidualIncome { get; set; }
        public string standardQmStatusCoveredLoan { get; set; }
        public string standardQmStatusSimultaneousLoan { get; set; }
        public string standardQmStatusMtgRelatedObligations { get; set; }
        public string standardQmStatusEmployment { get; set; }
        public string standardQmStatusIncome { get; set; }
        public string standardQmStatusAssets { get; set; }
        public string standardQmStatusDebtObligations { get; set; }
        public string standardQmStatusAlimony { get; set; }
        public string standardQmStatusChildSupport { get; set; }
        public string standardQmStatusCreditHistory { get; set; }
        public string gseagencyQmStatusOverall { get; set; }
        public string gseagencyQmStatusLoanTerm { get; set; }
        public string gseagencyQmStatusInterestOnly { get; set; }
        public string gseagencyQmStatusNegativeAmortization { get; set; }
        public string gseagencyQmStatusBalloonPayment { get; set; }
        public string gseagencyQmStatusPrepaymentPenalty { get; set; }
        public string gseagencyQmStatusPointsFeesLimit { get; set; }
        public string gseagencyQmStatusDti { get; set; }
        public string gseagencyQmStatusResidualIncome { get; set; }
        public string gseagencyQmStatusCoveredLoan { get; set; }
        public string gseagencyQmStatusSimultaneousLoan { get; set; }
        public string gseagencyQmStatusMtgRelatedObligations { get; set; }
        public string gseagencyQmStatusEmployment { get; set; }
        public string gseagencyQmStatusIncome { get; set; }
        public string gseagencyQmStatusAssets { get; set; }
        public string gseagencyQmStatusDebtObligations { get; set; }
        public string gseagencyQmStatusAlimony { get; set; }
        public string gseagencyQmStatusChildSupport { get; set; }
        public string gseagencyQmStatusCreditHistory { get; set; }
        public string gseagencyQmStatusSafeHarborEligibility { get; set; }
        public float initialMaxTotalPayment { get; set; }
        public float max5YrsTotalPayment { get; set; }
        public float max5YrsHousingRatio { get; set; }
        public float max5YrsTotalDebtRatio { get; set; }
        public float regZTotalLoanAmount { get; set; }
        public float currentQmFeeThresholdAmt { get; set; }
        public float currentQmFeeThresholdPercent { get; set; }
        public float max5YrsPrincipleAndInterest { get; set; }
        public float aporMaxBonaFideDiscountPoint { get; set; }
        public float rateReductionMaxBonaFideDiscountPoint { get; set; }
        public float startingAdjustedRateMaxBonaFideDiscountPoint { get; set; }
        public float bonaFideDiscountPoint { get; set; }
        public float bonaFideDiscountPointAmount { get; set; }
        public float initialRateMonthlyPayment { get; set; }
        public float initialRateHousingRatio { get; set; }
        public float initialRateTotalDebtRatio { get; set; }
        public string gseagencyQmStatusQmAvailable { get; set; }
        public float qmAprPercentage { get; set; }
        public bool usePriceBasedLimitTest { get; set; }
        public string standardQmStatusPriceLimit { get; set; }
    }

    public class Closingcost
    {
        public float disclosedSalesPrice { get; set; }
        public Closingdisclosure5 closingDisclosure5 { get; set; }
        public Gfe2010 gfe2010 { get; set; }
        public Gfe2010page gfe2010Page { get; set; }
        public Gfe2010section gfe2010Section { get; set; }
        public string impoundType3 { get; set; }
        public Loanestimate1 loanEstimate1 { get; set; }
        public Loanestimate2 loanEstimate2 { get; set; }
        public Loanestimate3 loanEstimate3 { get; set; }
        public Closingdisclosure1 closingDisclosure1 { get; set; }
        public Closingdisclosure2 closingDisclosure2 { get; set; }
        public Closingdisclosure3 closingDisclosure3 { get; set; }
        public Closingdisclosure4 closingDisclosure4 { get; set; }
        public Feevarianceother feeVarianceOther { get; set; }
    }

    public class Closingdisclosure5
    {
        public float totalPayments { get; set; }
        public string signatureType { get; set; }
    }

    public class Gfe2010
    {
        public float allOtherServiceAmount { get; set; }
        public float dailyInterestCharges { get; set; }
        public bool escrowChargeAllInsuranceIndicator { get; set; }
        public bool escrowChargeAllPropertyTaxesIndicator { get; set; }
        public bool escrowChargeOtherIndicator { get; set; }
        public Gfe2010fees[] gfe2010Fees { get; set; }
        public float homeownerInsurance { get; set; }
        public float initialLoanAmount { get; set; }
        public float initialMonthlyAmount { get; set; }
        public bool interestRateRiseIndicator { get; set; }
        public bool loanBalanceRiseIndicator { get; set; }
        public string loanOriginatorName { get; set; }
        public string maxLifeInterestCapPercentUi { get; set; }
        public bool monthlyPaymentRiseIndicator { get; set; }
        public float totalSettlementCharges { get; set; }
        public float section1400TotalBorrowerPaidAmount { get; set; }
        public float section900TotalBorrowerPaidAmount { get; set; }
        public float section900TotalPaidAmount { get; set; }
        public float section1400TotalPaidAmount { get; set; }
        public bool isCdValidForEarliestClosingDate { get; set; }
    }

    public class Gfe2010fees
    {
        public string gfe2010FeeParentType { get; set; }
        public string gfe2010FeeType { get; set; }
        public int gfe2010FeeIndex { get; set; }
        public bool pocPtcIndicator { get; set; }
        public bool sellerCreditIndicator2015 { get; set; }
        public float sec32PointsAndFees2015 { get; set; }
        public int lastDisclosedLoanEstimate2015 { get; set; }
        public bool borrowerCanShopForIndicator2015 { get; set; }
        public bool borrowerDidShopForIndicator2015 { get; set; }
        public float totalFeeAmount2015 { get; set; }
        public float totalFeePercentage2015 { get; set; }
        public float borrowerPtc2015 { get; set; }
        public float borrowerPac2015 { get; set; }
        public float borrowerAmountPaid2015 { get; set; }
    }

    public class Gfe2010page
    {
        public int balloonPaymentDueInYears { get; set; }
        public bool hasEscrowAccountIndicator { get; set; }
        public float hud1Pg1TotalSettlementCharges { get; set; }
        public float hud1Pg2TotalSettlementCharges { get; set; }
        public bool monthlyAmountIncludeInterestIndicator { get; set; }
        public bool monthlyAmountIncludePrincipalIndicator { get; set; }
        public float prepaidInterest { get; set; }
    }

    public class Gfe2010section
    {
        public bool loCompensationItemizeFeesIndicator { get; set; }
        public bool loCompensationUseLoCompensationToolIndicator { get; set; }
        public bool section1100ItemizeFeesIndicator { get; set; }
        public bool section800ItemizeFeesIndicator { get; set; }
        public string projectedPaymentTableType { get; set; }
        public string projectedPaymentTableColumns { get; set; }
        public float percentageDiscountPointsPaidByBorrower { get; set; }
    }

    public class Loanestimate1
    {
        public bool rangePaymentIndicatorC1 { get; set; }
        public bool rangePaymentIndicatorC2 { get; set; }
        public string leDateIssued { get; set; }
        public int loanTermMonths { get; set; }
        public string loanPurpose { get; set; }
        public string loanProduct { get; set; }
        public string rateLockExpirationTime { get; set; }
        public string rateLockExpirationTimeZone { get; set; }
        public string closingCostEstimateExpirationTime { get; set; }
        public string closingCostEstimateExpirationTimeZone { get; set; }
        public string closingCostEstimateExpirationDate { get; set; }
        public float estimatedTaxesInsuranceAssessments { get; set; }
        public string estimatedTaxesInsuranceAssessmentsUi { get; set; }
        public string inEscrowPropertyTaxes { get; set; }
        public string inEscrowHomeownerInsurance { get; set; }
        public string inEscrowOther { get; set; }
        public int ppc1Year { get; set; }
        public float ppc1MinimumPiPayment { get; set; }
        public string ppc1MinimumPiPaymentUi { get; set; }
        public float ppc1MaximumPiPayment { get; set; }
        public string ppc1MaximumPiPaymentUi { get; set; }
        public bool ppc1InterestOnly { get; set; }
        public string ppc1MiAmountUi { get; set; }
        public int ppc1EstimatedEscrowAmount { get; set; }
        public string ppc1EstimatedEscrowAmountUi { get; set; }
        public int ppc1MinimumMonthlyPayment { get; set; }
        public string ppc1MinimumMonthlyPaymentUi { get; set; }
        public float ppc1MaximumMonthlyPayment { get; set; }
        public string ppc1MaximumMonthlyPaymentUi { get; set; }
        public int ppc2YearTo { get; set; }
        public float ppc2MinimumPiPayment { get; set; }
        public string ppc2MinimumPiPaymentUi { get; set; }
        public float ppc2MaximumPiPayment { get; set; }
        public string ppc2MaximumPiPaymentUi { get; set; }
        public string ppc2MiAmountUi { get; set; }
        public string ppc2EstimatedEscrowAmountUi { get; set; }
        public int ppc2MinimumMonthlyPayment { get; set; }
        public string ppc2MinimumMonthlyPaymentUi { get; set; }
        public float ppc2MaximumMonthlyPayment { get; set; }
        public string ppc2MaximumMonthlyPaymentUi { get; set; }
        public bool ppestimatedEscrowIndicator { get; set; }
        public bool reasonSettlementCharges { get; set; }
        public bool reasonEligibility { get; set; }
        public bool reasonRevisions { get; set; }
        public bool reasonInterestRate { get; set; }
        public bool reasonExpiration { get; set; }
        public bool reasonDelayedSettlement { get; set; }
        public bool reasonOther { get; set; }
        public float totalEstimatedCashClose { get; set; }
        public string closingCostEstimateExpirationTimeUi { get; set; }
        public string closingCostEstimateExpirationTimeZoneUi { get; set; }
        public string closingCostEstimateExpirationDateUi { get; set; }
        public float initialMonthlyPaymentFor10000Loan { get; set; }
        public float maximumMonthlyPaymentFor10000Loan { get; set; }
        public int maximumPaymentMonth { get; set; }
        public float initialMonthlyPaymentFor60000Loan { get; set; }
        public string balloonPaymentDueInTermLabel { get; set; }
        public string interestRateAdjTermLabel { get; set; }
        public string expirationGenericTimeZone { get; set; }
    }

    public class Loanestimate2
    {
        public int originationChargesSubTotal { get; set; }
        public int servicesYouNotShopSubTotal { get; set; }
        public int servicesYouShopSubTotal { get; set; }
        public int totalLoanCosts { get; set; }
        public int taxesGovFeesSubTotal { get; set; }
        public int prepaidsSubTotal { get; set; }
        public int initialEscrowPaymentClosingSubTotal { get; set; }
        public int otherSubTotal { get; set; }
        public int totalOtherCosts { get; set; }
        public int totalClosingCosts { get; set; }
        public int totalLoanAndOtherCosts { get; set; }
        public float closingCostsFinanced { get; set; }
        public float downPayment { get; set; }
        public float fundsForBorrower { get; set; }
        public float adjustmentsOtherCredits { get; set; }
        public float estimatedCashToCloseSv { get; set; }
        public float estimatedCashToCloseAv { get; set; }
        public string fromOrToBorrower { get; set; }
        public bool useAlternate { get; set; }
        public int sellerCreditAmount { get; set; }
        public float unroundedTotalLoanCosts { get; set; }
        public float unroundedTotalOtherCosts { get; set; }
        public float actualStdleTotalClosingCostJ { get; set; }
        public float actualStdleSellerCredits { get; set; }
        public float thirdPartyPaymentsNotOtherwiseDisclosed { get; set; }
        public bool useActualDownPaymentAndClosingCostsFinancedIndicator { get; set; }
        public int estimatedTotalPayoffsAndPaymentsAmount { get; set; }
    }

    public class Loanestimate3
    {
        public string assumption { get; set; }
        public float totalInterestPercentage { get; set; }
        public string totalInterestPercentageUi { get; set; }
        public int in5YearsTotalYouWillHavePaid { get; set; }
        public int in5YearsPrincipalYouWillHavePaidOff { get; set; }
        public string signatureType { get; set; }
    }

    public class Closingdisclosure1
    {
        public bool rangePaymentIndicatorC1 { get; set; }
        public bool rangePaymentIndicatorC2 { get; set; }
        public float estimatedTaxesInsuranceAssessments { get; set; }
        public string inEscrowPropertyTaxes { get; set; }
        public string inEscrowHomeownerInsurance { get; set; }
        public string inEscrowOther { get; set; }
        public int ppc1Year { get; set; }
        public float ppc1MinimumPiPayment { get; set; }
        public string ppc1MinimumPiPaymentUi { get; set; }
        public float ppc1MaximumPiPayment { get; set; }
        public string ppc1MaximumPiPaymentUi { get; set; }
        public bool ppc1InterestOnly { get; set; }
        public float ppc1EstimatedEscrowAmount { get; set; }
        public float ppc1MinimumMonthlyPayment { get; set; }
        public string ppc1MinimumMonthlyPaymentUi { get; set; }
        public float ppc1MaximumMonthlyPayment { get; set; }
        public string ppc1MaximumMonthlyPaymentUi { get; set; }
        public int ppc2YearTo { get; set; }
        public float ppc2MinimumPiPayment { get; set; }
        public string ppc2MinimumPiPaymentUi { get; set; }
        public float ppc2MaximumPiPayment { get; set; }
        public string ppc2MaximumPiPaymentUi { get; set; }
        public float ppc2MinimumMonthlyPayment { get; set; }
        public string ppc2MinimumMonthlyPaymentUi { get; set; }
        public float ppc2MaximumMonthlyPayment { get; set; }
        public string ppc2MaximumMonthlyPaymentUi { get; set; }
        public bool ppcMortgageInsuranceIndicator { get; set; }
        public bool reasonChangeInApr { get; set; }
        public bool reasonChangeInLoanProduct { get; set; }
        public bool reasonPrepaymentPenalty { get; set; }
        public bool reasonChangeSettlementCharges { get; set; }
        public bool reasonAdvancedReview { get; set; }
        public bool reasonToleranceCure { get; set; }
        public bool reasonClericalErrorCorrection { get; set; }
        public bool reasonOther { get; set; }
        public bool changedCircumstanceFlag { get; set; }
        public bool reasonRevisionsReqConsumer { get; set; }
        public bool reasonInterestRatecharges { get; set; }
        public bool reasonChangedCircumstanceElg { get; set; }
        public float totalCashToClose { get; set; }
        public string ppc1MiAmountUi { get; set; }
        public string ppc1EstimatedEscrowAmountUi { get; set; }
        public string ppc2MiAmountUi { get; set; }
        public string ppc2EstimatedEscrowAmountUi { get; set; }
    }

    public class Closingdisclosure2
    {
        public float originationChargesSubTotal { get; set; }
        public float servicesDidNotShopSubTotal { get; set; }
        public float servicesDidShopSubTotal { get; set; }
        public float totalLoanCost { get; set; }
        public float totalBorrowerPaidAtClosing { get; set; }
        public float totalBorrowerPaidBeforeClosing { get; set; }
        public float taxesGovermentFeesSubTotal { get; set; }
        public float prepaidsSubTotal { get; set; }
        public float initialEscrowSubTotal { get; set; }
        public float otherSubTotal { get; set; }
        public float totalOtherCost { get; set; }
        public float totalOtherCostAtClosing { get; set; }
        public float totalOtherCostBeforeClosing { get; set; }
        public float totalClosingCost { get; set; }
        public float borrowerClosingCostAtClosing { get; set; }
        public float borrowerClosingCostBeforeClosing { get; set; }
        public float sellerClosingCostAtClosing { get; set; }
        public float sellerClosingCostBeforeClosing { get; set; }
        public float closingCostPaidByOthers { get; set; }
    }

    public class Closingdisclosure3
    {
        public float closingCostsPaidAtClosing { get; set; }
        public float totalDuefromBorrowerAtClosing { get; set; }
        public float totalPaidAlreadybyoronBehalfofBoroweratClosing { get; set; }
        public float cashToClose { get; set; }
        public float totalDuetoSelleratClosingM { get; set; }
        public float totalDuefromSelleratClosingN { get; set; }
        public float cash { get; set; }
        public float totalFromK { get; set; }
        public float totalFromL { get; set; }
        public float totalFromM { get; set; }
        public float totalFromN { get; set; }
        public float finalCashToClose { get; set; }
        public float closingCostsPaidatClosingJ { get; set; }
        public string fromToBorrower { get; set; }
        public string fromToSeller { get; set; }
        public float liabilityTotal { get; set; }
        public float loanAmount { get; set; }
        public float cd3TotalClosingCostJ { get; set; }
        public float cd3ClosingCostsPaidBeforeClosing { get; set; }
        public float cd3TotalPayoffsAndPaymentsK { get; set; }
        public float cd3CashToClose { get; set; }
        public string cd3CashToCloseFromToBorrower { get; set; }
        public float leLoanAmount { get; set; }
        public float lecd3TotalClosingCostJ { get; set; }
        public float lecd3ClosingCostsPaidBeforeClosing { get; set; }
        public float lecd3TotalPayoffsAndPaymentsK { get; set; }
        public float lecd3CashToClose { get; set; }
        public string lecd3CashToCloseFromToBorrower { get; set; }
        public float stdleTotalClosingCostJ { get; set; }
        public float stdleCd3ClosingCostsPaidBeforeClosing { get; set; }
        public float stdleClosingCostFinanced { get; set; }
        public float stdleDownPayment { get; set; }
        public float stdleDeposit { get; set; }
        public float stdleFundForBorrower { get; set; }
        public float stdleSellerCredits { get; set; }
        public float stdleAdjustmentAndOtherCredits { get; set; }
        public float stdleCashToClose { get; set; }
        public float stdFinalTotalClosingCostJ { get; set; }
        public float stdFinalCd3ClosingCostsPaidBeforeClosing { get; set; }
        public float stdFinalClosingCostFinanced { get; set; }
        public float stdFinalDownPayment { get; set; }
        public float stdFinalDeposit { get; set; }
        public float stdFinalFundForBorrower { get; set; }
        public float stdFinalSellerCredits { get; set; }
        public float stdFinalAdjustmentAndOtherCredits { get; set; }
        public string stdTotalClosingCostDidChangeCol { get; set; }
        public string stdTotalClosingCostBeforeClosingDidChangeCol { get; set; }
        public string stdDownPaymentDidChangeCol { get; set; }
        public string stdDepositDidChangeCol { get; set; }
        public string stdFundsForBorrowerDidChangeCol { get; set; }
        public string stdSellerCreditsDidChangeCol { get; set; }
        public string stdAdjustmentsDidChangeCol { get; set; }
        public string altLoanAmountDidChangeCol { get; set; }
        public string altTotalClosingCostDidChangeCol { get; set; }
        public string altClosingCostBeforeClosingDidChangeCol { get; set; }
        public string altTotalPayoffsDidChangeCol { get; set; }
        public string altCashToCloseDidChangeCol { get; set; }
        public string stdTotalClosingCostRemark { get; set; }
        public string stdDownPaymentIncDecRemark { get; set; }
        public string stdClosingCostFinancedDidChangeCol { get; set; }
        public string stdDownPaymentSectionRemark { get; set; }
        public string altLoanAmountIncDecRemark { get; set; }
        public string altTotalClosingCostRemark { get; set; }
        public float actualLeLoanAmountFromLatestRec { get; set; }
        public float actualLecd3TotalClosingCostJFromLatestRec { get; set; }
        public float actualLecd3TotalPayoffsAndPaymentsKFromLatestRec { get; set; }
        public float actualStdleTotalClosingCostJFromLatestRec { get; set; }
        public float actualStdleClosingCostFinancedFromLatestRec { get; set; }
        public float actualStdleDownPaymentFromLatestRec { get; set; }
        public float actualStdleDepositFromLatestRec { get; set; }
        public float actualStdleFundForBorrowerFromLatestRec { get; set; }
        public float actualStdleSellerCreditsFromLatestRec { get; set; }
        public float actualStdleAdjustmentAndOtherCreditsFromLatestRec { get; set; }
        public bool excludeBorrowerClosingCosts { get; set; }
        public float ucdkSubTotal { get; set; }
        public float ucdlSubTotal { get; set; }
        public float ucdTotalAdjustmentsAndOtherCredits { get; set; }
        public float nonUcdTotalAdjustmentsAndOtherCredits { get; set; }
        public float totalAdjustmentsAndOtherCredits { get; set; }
        public bool newVerbiageDisclosed { get; set; }
    }

    public class Closingdisclosure4
    {
        public float monthlyEscrowPayment { get; set; }
        public bool interestOnlyPayments { get; set; }
        public bool stepPayments { get; set; }
        public bool seasonalPayments { get; set; }
        public bool ignoreArmAdj { get; set; }
        public string escrowedPropertyCostsBasis { get; set; }
    }

    public class Feevarianceother
    {
        public float appliedCureAmount { get; set; }
        public bool leBaselineUsedCannotDecrease { get; set; }
        public bool leBaselineUsedCannotIncrease { get; set; }
        public bool leBaselineUsedCannotIncrease10 { get; set; }
    }

    public class Closingdocument
    {
        public Additionalstatedisclosure[] additionalStateDisclosures { get; set; }
        public string closingProvider { get; set; }
        public string compliancePropertyIdentifiedDate { get; set; }
        public string disbursementDate { get; set; }
        public float disbursementsToBorrower { get; set; }
        public string lastAuditDate { get; set; }
        public string lastDocumentOrdered { get; set; }
        public string docReportGuid { get; set; }
        public Respahuddetail[] respaHudDetails { get; set; }
        public Statelicens[] stateLicenses { get; set; }
        public bool syncInterestDateDisbursementDate { get; set; }
    }

    public class Additionalstatedisclosure
    {
        public string stateCode { get; set; }
        public string disclosureName { get; set; }
        public object disclosureValue { get; set; }
    }

    public class Respahuddetail
    {
        public int lineNumber { get; set; }
        public float lineItemAmount { get; set; }
        public float realValue { get; set; }
    }

    public class Statelicens
    {
        public string stateLicenseType { get; set; }
        public string ak { get; set; }
        public string al { get; set; }
        public string ar { get; set; }
        public string az { get; set; }
        public string ca { get; set; }
        public string co { get; set; }
        public string ct { get; set; }
        public string dc { get; set; }
        public string de { get; set; }
        public string fl { get; set; }
        public string ga { get; set; }
        public string gu { get; set; }
        public string hi { get; set; }
        public string ia { get; set; }
        public string idaho { get; set; }
        public string il { get; set; }
        public string _in { get; set; }
        public string ks { get; set; }
        public string ky { get; set; }
        public string la { get; set; }
        public string ma { get; set; }
        public string md { get; set; }
        public string me { get; set; }
        public string mi { get; set; }
        public string mn { get; set; }
        public string mo { get; set; }
        public string ms { get; set; }
        public string mt { get; set; }
        public string nc { get; set; }
        public string nd { get; set; }
        public string ne { get; set; }
        public string nh { get; set; }
        public string nj { get; set; }
        public string nm { get; set; }
        public string nv { get; set; }
        public string ny { get; set; }
        public string oh { get; set; }
        public string ok { get; set; }
        public string or { get; set; }
        public string pa { get; set; }
        public string pr { get; set; }
        public string ri { get; set; }
        public string sc { get; set; }
        public string sd { get; set; }
        public string tn { get; set; }
        public string tx { get; set; }
        public string ut { get; set; }
        public string va { get; set; }
        public string vi { get; set; }
        public string vt { get; set; }
        public string wa { get; set; }
        public string wi { get; set; }
        public string wv { get; set; }
        public string wy { get; set; }
    }

    public class Commitmentterms
    {
        public string mortgageeName { get; set; }
    }

    public class Constructionmanagement
    {
    }

    public class Correspondent
    {
        public int gracePeriodDays { get; set; }
        public float lateFeePercentage { get; set; }
        public float additionalLateFeeCharge { get; set; }
        public int lfsCalculateAs { get; set; }
        public int lfsFeeHandledAs { get; set; }
        public int lfsGracePeriodCalendar { get; set; }
        public int lfsGracePeriodLaterOf { get; set; }
        public int lfsGracePeriodStarts { get; set; }
        public int lfsIncludeDay { get; set; }
        public int lfsLateFeeBasedOn { get; set; }
        public int lfsMaxLateDays { get; set; }
        public int lfsStartOnWeekend { get; set; }
        public int lfsGracePeriodDays { get; set; }
        public int lfsDayCleared { get; set; }
        public string lateDaysEndTrigger { get; set; }
        public float lfsLateFee { get; set; }
        public float lfsAmount { get; set; }
    }

    public class Custommodelfields
    {
    }

    public class Disclosurenotices
    {
    }

    public class Downpayment
    {
        public float amount { get; set; }
    }

    public class Locompensation
    {
        public string whoPaidCompensation { get; set; }
    }

    public class Emdocument
    {
    }

    public class Emdocumentinvestor
    {
    }

    public class Emdocumentlender
    {
    }

    public class Fanniemae
    {
        public float cltv { get; set; }
        public float hcltv { get; set; }
        public float ltv { get; set; }
    }

    public class Fhavaloan
    {
        public Energyefficientmortgage energyEfficientMortgage { get; set; }
        public float minimumDownPayment { get; set; }
        public float prepaidExpenses { get; set; }
        public int proposedMaturityMonths { get; set; }
        public float totalClosingCost { get; set; }
        public bool useDefaultLenderInfo { get; set; }
        public float fhaSalesPrice { get; set; }
        public float fhaMortgageAmount { get; set; }
        public float fhaMortgageAmountUmip { get; set; }
        public float fhaClosingCost { get; set; }
        public float fhaDownPayment { get; set; }
        public float fhaInterestRate { get; set; }
        public int fhaLoanTerm { get; set; }
        public float fhaMonthlyPayment { get; set; }
        public float fhaLtv { get; set; }
        public float fhaMmi { get; set; }
        public int fhaYearsMonthlyInsurance { get; set; }
        public float fhaUmip { get; set; }
        public float conventionalSalesPrice { get; set; }
        public float conventionalMortgageAmount { get; set; }
        public float conventionalClosingCost { get; set; }
        public float conventionalDownPayment { get; set; }
        public float conventionalInterestRate { get; set; }
        public int conventionalLoanTerm { get; set; }
        public float conventionalMonthlyPayment { get; set; }
        public float conventionalLtv { get; set; }
        public float conventionalMmi { get; set; }
        public int conventionalMaxYearsMi { get; set; }
    }

    public class Energyefficientmortgage
    {
        public float appraisedValue { get; set; }
        public float backRatio { get; set; }
    }

    public class Freddiemac
    {
        public float allMonthlyPayments { get; set; }
        public float netPurchasePrice { get; set; }
        public float originalIntRate { get; set; }
        public float reserves { get; set; }
        public float ltv { get; set; }
        public float tltv { get; set; }
        public float htltv { get; set; }
    }

    public class Funding
    {
        public string fundingFees { get; set; }
    }

    public class Gfe
    {
        public string address { get; set; }
        public string brokerName { get; set; }
        public string brokerRepresentative { get; set; }
        public string city { get; set; }
        public float estimatedCashToBorrower { get; set; }
        public string estimatedDueDate { get; set; }
        public float finalBalloonPayment { get; set; }
        public Gfefee[] gfeFees { get; set; }
        public bool hasPrepaymentPenaltyIndicator { get; set; }
        public float monthlyPaymentAndEscrow { get; set; }
        public string postalCode { get; set; }
        public float principalAmount { get; set; }
        public float purchasePayOff { get; set; }
        public string state { get; set; }
        public float totalPrepaidClosingCost { get; set; }
    }

    public class Gfefee
    {
        public string gfeFeeType { get; set; }
        public int gfeFeeIndex { get; set; }
        public float otherAmount { get; set; }
    }

    public class Hmda
    {
        public string balloonIndicator { get; set; }
        public bool cdRequired { get; set; }
        public string countyCode { get; set; }
        public string hoepaStatus { get; set; }
        public string hmda2InterestOnlyIndicator { get; set; }
        public string loanBalanceRiseIndicator { get; set; }
        public string msaNumber { get; set; }
        public string stateCode { get; set; }
        public int reportingYear { get; set; }
        public string applicationDate { get; set; }
        public string loanType { get; set; }
        public float loanAmount { get; set; }
        public string businessOrCommercialPurpose { get; set; }
        public string totalLoanCosts { get; set; }
        public string totalPointsAndFees { get; set; }
        public string originationCharges { get; set; }
        public string prepaymentPenaltyPeriod { get; set; }
        public string loanTerm { get; set; }
        public string introRatePeriod { get; set; }
        public string propertyValue { get; set; }
        public string nmlsLoanOriginatorId { get; set; }
        public string hmdaPropertyZipCode { get; set; }
        public string hmdaPropertyAddress { get; set; }
        public string hmdaPropertyCity { get; set; }
        public string hmdaPropertyState { get; set; }
        public bool hmdaSyncAddressIndicator { get; set; }
        public string hmdaProfileApplicationDateValue { get; set; }
        public bool hmdaInterestOnlyIndicator { get; set; }
        public string hmdaCountyCode { get; set; }
    }

    public class Hud1es
    {
        public float biweeklyPiti { get; set; }
        public float biweeklyTotalBiweeklyPayment { get; set; }
        public Hud1esdates[] hud1EsDates { get; set; }
        public Hud1esduedates[] hud1EsDueDates { get; set; }
        public Hud1esitemizes[] hud1EsItemizes { get; set; }
        public int hud1EsItemizesTotalLines { get; set; }
        public Hud1essetups[] hud1EsSetups { get; set; }
        public float startingBalance { get; set; }
        public float escrowPaymentYearly { get; set; }
        public float nonEscrowCostsYearly { get; set; }
        public string escrowFirstPaymentDate { get; set; }
        public string escrowFirstPaymentDateType { get; set; }
    }

    public class Hud1esdates
    {
        public string id { get; set; }
        public string date { get; set; }
    }

    public class Hud1esduedates
    {
        public string id { get; set; }
    }

    public class Hud1esitemizes
    {
        public string id { get; set; }
        public string date { get; set; }
    }

    public class Hud1essetups
    {
        public string id { get; set; }
        public string date { get; set; }
    }

    public class Hudloandata
    {
        public float contractSalesPrice { get; set; }
        public float lesserOfSalesPrice { get; set; }
        public string loanPurpose { get; set; }
        public float supplementalOriginationFee { get; set; }
        public float financeableMortgageFeesIfCharged { get; set; }
        public float totalRehabilitationCostsFeesReserves { get; set; }
        public float purchasePriceLessInducementToPurchase { get; set; }
        public float step2EPlusStep1E { get; set; }
        public float appropriateLtvFactor { get; set; }
        public float initialDrawAtClosingTotal { get; set; }
        public float rehabilitationEscrowAccount { get; set; }
        public string criteriaForAppropriateLtvFactor { get; set; }
    }

    public class Interimservicing
    {
        public string calcTriggered { get; set; }
        public float beginningBalance { get; set; }
    }

    public class Loanproductdata
    {
        public bool balloonIndicator { get; set; }
        public int balloonLoanMaturityTermMonthsCount { get; set; }
        public string floorPercentUi { get; set; }
        public string indexMarginPercentUi { get; set; }
        public string lienPriorityType { get; set; }
        public string loanDocumentationType { get; set; }
        public float maximumMonthlyPayment { get; set; }
        public float maxLifeInterestCapPercent { get; set; }
        public string maxLifeInterestCapPercentUi { get; set; }
        public string nmlsDocumentationType { get; set; }
        public string nmlsFirstMortgageType { get; set; }
        public string nmlsLoanType { get; set; }
        public bool nmlsOptionArmIndicator { get; set; }
        public bool nmlsPiggyBackOrFundedHelocIndicator { get; set; }
        public string nmlsRefinancePurposeType { get; set; }
        public float initialApplicationAmount { get; set; }
        public bool prepaymentPenaltyIndicator { get; set; }
        public string rateAdjustmentPercentUi { get; set; }
        public string rateAdjustmentSubsequentCapPercentUi { get; set; }
        public string scheduledFirstPaymentDate { get; set; }
        public string helocLienPosition { get; set; }
        public bool helocEscrowAccountIndicator { get; set; }
        public bool helocDrawPaymentBasis { get; set; }
        public bool helocRepaymentBasis { get; set; }
        public bool helocTaRequestAdvanceInPersonIndicator { get; set; }
        public bool helocTaRequestAdvanceViaInternetIndicator { get; set; }
        public bool helocTaPeriodicCapAppliedToRepaymentPeriod { get; set; }
        public bool helocTaInstallmentLoanDiscountedAprIndicator { get; set; }
    }

    public class Loansubmission
    {
        public float amountAvailable { get; set; }
        public float amountRequiredToClose { get; set; }
    }

    public class Mcaw
    {
        public float adjustedPurchasePrice { get; set; }
        public float cashReserves { get; set; }
        public float contractSalesPrice { get; set; }
        public float lesserSalesPrice { get; set; }
        public float mortgageBasisPurchase { get; set; }
        public float mortgageBasisRefinance { get; set; }
        public float sixPercentOfLineA1 { get; set; }
        public float maximumSellerContribution4Percent { get; set; }
        public float statutoryInvestment { get; set; }
        public float totalCashToClose { get; set; }
        public float unadjustedAcquisition { get; set; }
    }

    public class Miscellaneous
    {
        public string complianceVersionCd3X1505 { get; set; }
        public string loanProgramFile { get; set; }
        public bool newBuyDownEnabled { get; set; }
        public string contactName { get; set; }
        public string contactPhone { get; set; }
        public int loanFileSequenceNumber { get; set; }
        public bool tpoConnectStatusUpdated { get; set; }
        public string savedLogVersion { get; set; }
    }

    public class Nettangiblebenefit
    {
        public float existingLoanPaymentDifference { get; set; }
        public float newLoanPaymentWithMiDifference { get; set; }
        public float newLoanPaymentWithObligationDifference { get; set; }
    }

    public class Prequalification
    {
        public float afterTaxOwnMoSavings { get; set; }
        public float avgMoPmtSavings { get; set; }
        public float beforeTaxOwnMoPmt { get; set; }
        public float combinedGain { get; set; }
        public string favorableOption { get; set; }
        public float homeSellingPriceAfterYears { get; set; }
        public float investmentGain { get; set; }
        public float minusDownPmtAndClosingCosts { get; set; }
        public float minusLoanBalance { get; set; }
        public int monthlySavings { get; set; }
        public Prequalificationscenario[] prequalificationScenarios { get; set; }
        public string qualificationStatus { get; set; }
        public float totalGain { get; set; }
        public int totalLoanSavings { get; set; }
        public float totalOwnPmtOverYears { get; set; }
        public float totalPmtSavings { get; set; }
        public string withinLimits9 { get; set; }
    }

    public class Prequalificationscenario
    {
        public int prequalificationScenarioIndex { get; set; }
        public float totalBaseCost { get; set; }
        public float totalCashLeft { get; set; }
        public string currentStatus { get; set; }
    }

    public class Privacypolicy
    {
        public string month { get; set; }
        public int year { get; set; }
    }

    public class Profitmanagement
    {
    }

    public class Property1
    {
        public string addressLineText { get; set; }
        public string city { get; set; }
        public string county { get; set; }
        public bool fhaSecondaryResidenceIndicator { get; set; }
        public string loanPurposeType { get; set; }
        public string loanPurposeTypeUrla { get; set; }
        public string nameRecordingJurisdiction { get; set; }
        public string occupancyDisplayField { get; set; }
        public string postalCode { get; set; }
        public string propertyUsageType { get; set; }
        public string state { get; set; }
        public string streetAddress { get; set; }
        public string typeRecordingJurisdiction { get; set; }
    }

    public class Ratelock
    {
        public float amountDue { get; set; }
        public string amountDueTo { get; set; }
        public string amountPaidTo { get; set; }
        public int balloonLoanMaturityTermMonths { get; set; }
        public float baseLoanAmount { get; set; }
        public float borrowerRequestedLoanAmount { get; set; }
        public float combinedLtv { get; set; }
        public float diffAmountReceived { get; set; }
        public float diffPrinciple { get; set; }
        public float expectedAmountReceived { get; set; }
        public float expectedPrinciple { get; set; }
        public bool fhaSecondaryResidence { get; set; }
        public float firstSubordinateAmount { get; set; }
        public float hcltvHtltv { get; set; }
        public float helocCreditLimit { get; set; }
        public string subjectPropertyPropertyStreetAddress { get; set; }
        public string lienPriorityType { get; set; }
        public int loanAmortizationTermMonths { get; set; }
        public string loanAmortizationType { get; set; }
        public string loanDocumentationType { get; set; }
        public string loanProgram { get; set; }
        public string loanProgramFile { get; set; }
        public Lockrequestborrower[] lockRequestBorrowers { get; set; }
        public string lockRequestLoanPurposeType { get; set; }
        public float ltv { get; set; }
        public string mortgageType { get; set; }
        public int propertyAppraisedValueAmount { get; set; }
        public int propertyEstimatedValueAmount { get; set; }
        public string propertyUsageType { get; set; }
        public float purchasePriceAmount { get; set; }
        public float reconciledDiff { get; set; }
        public string requestPrepayPenalty { get; set; }
        public string subjectPropertyCity { get; set; }
        public string subjectPropertyCounty { get; set; }
        public string subjectPropertyPostalCode { get; set; }
        public string subjectPropertyState { get; set; }
        public string subjectPropertyStreetAddress { get; set; }
        public bool isDeliveryType { get; set; }
        public int correspondentWarehouseBankId { get; set; }
        public int daysToExtend { get; set; }
        public string correspondentPaymentHistoryFirstBorrowerPaymentDueDate { get; set; }
        public string rateRequestStatus { get; set; }
        public string requestLockType { get; set; }
        public string requestLockStatus { get; set; }
        public string isCancelled { get; set; }
        public string rateStatus { get; set; }
        public string requestPending { get; set; }
        public string requestType { get; set; }
        public string extensionRequestPending { get; set; }
        public string cancellationRequestPending { get; set; }
        public string reLockRequestPending { get; set; }
        public string borrLenderPaid { get; set; }
        public bool correspondentRetainUserInputs { get; set; }
    }

    public class Lockrequestborrower
    {
        public int lrbIndex { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string ssn { get; set; }
    }

    public class Regulationz
    {
        public bool allDateAndNumericalDisclosuresIndicator { get; set; }
        public float amountFinanced { get; set; }
        public float aprPercent { get; set; }
        public string aprPercentUi { get; set; }
        public string assumptionOnYourProperty { get; set; }
        public bool borrowerIntendToContinueIndicator { get; set; }
        public string borrowerMiTerminationDate { get; set; }
        public string dueDate { get; set; }
        public float financeCharge { get; set; }
        public string gfeApplicationDate { get; set; }
        public bool gfeChangedCircumstanceIndicator { get; set; }
        public bool gfeRateLockRedisclosureRequiredIndicator { get; set; }
        public bool hud1ToleranceViolatedIndicator { get; set; }
        public string initialDisclosureDueDate { get; set; }
        public string interestInNameOf { get; set; }
        public bool interestOnlyIndicator { get; set; }
        public int lateChargeDays { get; set; }
        public float lateChargeBasis { get; set; }
        public string lateChargeType { get; set; }
        public float maximumPayment { get; set; }
        public bool meansAnEstimateIndicator { get; set; }
        public float minimumPayment { get; set; }
        public string miScheduledTerminationDate { get; set; }
        public string namePreparedBy { get; set; }
        public int numberOfPayments { get; set; }
        public float outstandingBalance { get; set; }
        public string paymentFrequencyType { get; set; }
        public string pmiMidpointCancelationDate { get; set; }
        public bool propertyInsuranceIndicator { get; set; }
        public bool refundPaymentIndicator { get; set; }
        public Regulationzinterestrateperiod[] regulationZInterestRatePeriods { get; set; }
        public Regulationzpayment[] regulationZPayments { get; set; }
        public string regzTableType { get; set; }
        public float samplePayments { get; set; }
        public float totalOfPayments { get; set; }
        public string useDaysInYears { get; set; }
        public string yearOfMaximumPayment { get; set; }
        public string leReceivedBy { get; set; }
        public string leReceivedMethod { get; set; }
        public string leReceivedMethodOther { get; set; }
        public string eSignConsentDate { get; set; }
        public string eSignConsentBorrowerStatus1 { get; set; }
        public string eSignConsentBorrowerDateAccepted1 { get; set; }
        public string eSignConsentBorrowerIpAddress1 { get; set; }
        public string eSignConsentBorrowerSource1 { get; set; }
        public string eSignConsentCoBorrowerStatus1 { get; set; }
        public string eSignConsentCoBorrowerDateAccepted1 { get; set; }
        public string eSignConsentCoBorrowerIpAddress1 { get; set; }
        public string eSignConsentCoBorrowerSource1 { get; set; }
        public float totalOfPrincipalAndInterest { get; set; }
        public string externaleConsent { get; set; }
        public string zeroPercentPaymentOption { get; set; }
        public string interestAccrual { get; set; }
    }

    public class Regulationzinterestrateperiod
    {
        public string regulationZInterestRatePeriodType { get; set; }
        public float monthlyPayment { get; set; }
        public float totalPayment { get; set; }
    }

    public class Regulationzpayment
    {
        public int regulationZPaymentIndex { get; set; }
        public float balance { get; set; }
        public float interestRatePercent { get; set; }
        public float monthlyPayment { get; set; }
        public int numberOfPayments { get; set; }
        public string paymentDate { get; set; }
    }

    public class Section32
    {
        public string loanQualifyAsHighCostMortgage { get; set; }
        public float maximumPercentageOfLoan { get; set; }
        public float maximumPointsAndFees { get; set; }
        public string resultOfPointAndFees { get; set; }
        public string resultOfSecurityYieldTest { get; set; }
        public string exceed2PercentPrepayPenalty { get; set; }
    }

    public class Selectedhomecounselingprovider
    {
    }

    public class Servicingdisclosure
    {
        public bool weDoNotServiceMortgageLoansIndicator { get; set; }
    }

    public class Shipping
    {
        public string ecUpdatesAllowed { get; set; }
    }

    public class Statedisclosure
    {
        public string nameOfLicensee { get; set; }
    }

    public class Statementcreditdenial
    {
    }

    public class Tpo
    {
        public string preferredAus { get; set; }
    }

    public class Tql
    {
        public int tqlFraudAlertsTotal { get; set; }
        public int tqlFraudAlertsTotalHigh { get; set; }
        public int tqlFraudAlertsTotalMedium { get; set; }
        public int tqlFraudAlertsTotalLow { get; set; }
        public int tqlFraudAlertsTotalHighUnaddressed { get; set; }
        public int tqlFraudAlertsTotalMediumUnaddressed { get; set; }
        public int tqlFraudAlertsTotalLowUnaddressed { get; set; }
        public int driveScore { get; set; }
        public int driveIdVerifyScore { get; set; }
        public int driveAppVerifyScore { get; set; }
        public int drivePropertyVerifyScore { get; set; }
        public bool lomaOrLomrIndicator { get; set; }
    }

    public class Trustaccount
    {
    }

    public class Tsum
    {
        public float verified { get; set; }
    }

    public class Uldd
    {
        public string coBorrowerCountryCode { get; set; }
        public string countryCode { get; set; }
        public int fannieCltv { get; set; }
        public string fannieFloodSpecialFeatureCode { get; set; }
        public int fannieHclTv { get; set; }
        public int fannieInvestorOwnershipPercent { get; set; }
        public float fannieLenderPaidMiInterestRateAdjustmentPercent { get; set; }
        public int fannieLtv { get; set; }
        public int fanniePoolOwnershipPercent { get; set; }
        public float fannieRateSpread { get; set; }
        public int fannieTlTv { get; set; }
        public string fannieMortgageType { get; set; }
        public string freddieMortgageType { get; set; }
        public string sellerLoanIdentifier { get; set; }
        public bool temporaryBuydownIndicator { get; set; }
        public string firstRateChangePaymentEffectiveDate { get; set; }
        public int fannieBltv { get; set; }
        public int totalMortgagedPropertiesCount { get; set; }
        public float freddieLtv { get; set; }
        public float freddieTltv { get; set; }
        public float freddieHtltv { get; set; }
    }

    public class Underwritersummary
    {
    }

    public class Usda
    {
        public bool refinanceLoanIndicator { get; set; }
        public float totalAnnualIncome { get; set; }
        public float totalAdjustedAnnualIncome { get; set; }
    }

    public class Valoandata
    {
        public float vaRecoupmentMonthlyDecreaseInPayment { get; set; }
        public float vaRecoupmentTotalClosingCosts { get; set; }
        public float totalProposedMonthlyPayment { get; set; }
        public float proposedHomeEquityRemainingAmount { get; set; }
        public float cashDisbursedToBorrowerAmount { get; set; }
        public float disbursementsLessPayoffAmount { get; set; }
        public float totalClosingCostLessGuaranteeFee { get; set; }
        public bool includeSectionAIndicator { get; set; }
        public bool includeSectionBVaFundingFeeIndicator { get; set; }
        public bool includeSectionBOtherIndicator { get; set; }
        public bool includeSectionCIndicator { get; set; }
        public bool includeSectionEIndicator { get; set; }
        public bool includeSectionFIndicator { get; set; }
        public bool includeSectionGIndicator { get; set; }
        public bool includeSectionHIndicator { get; set; }
        public bool includeLenderCreditIndicator { get; set; }
    }

    public class Collateraltracking
    {
    }

    public class Aiq
    {
    }

    public class Eclose
    {
    }

    public class Application
    {
        public string id { get; set; }
        public string legacyId { get; set; }
        public Vod[] vods { get; set; }
        public float assetsAvailableAmount { get; set; }
        public Borrower borrower { get; set; }
        public float bottomRatioPercent { get; set; }
        public Coborrower coborrower { get; set; }
        public float fhaVaDebtIncomeRatio { get; set; }
        public float fhaVaFamilySupportAmount { get; set; }
        public float fhaVaTotalEstimatedMonthlyShelterExpenseAmount { get; set; }
        public float fhaVaTotalNetEffectiveIncomeAmount { get; set; }
        public float fhaVaTotalNetIncomeAmount { get; set; }
        public float fhaVaTotalNetTakeHomePayAmount { get; set; }
        public float freddieMacOccupantHousingRatio { get; set; }
        public float freddieMacOccupantDebtRatio { get; set; }
        public float grossBaseIncomeAmount { get; set; }
        public Income[] income { get; set; }
        public bool incomeOtherThanBorrowerUsedIndicator { get; set; }
        public bool jointAssetLiabilityReportingIndicator { get; set; }
        public float mcawGrossMonthlyIncomeAmount { get; set; }
        public float mcawMortgagePaymentToIncome1Amount { get; set; }
        public float mcawTotalFixedPaymentForPurchaseAmount { get; set; }
        public float mcawTotalFixedPaymentToIncome1Amount { get; set; }
        public float mcawTotalMortgagePaymentAmount { get; set; }
        public float monthlyHousingExpenseAmount { get; set; }
        public float netWorthAmount { get; set; }
        public string propertyUsageType { get; set; }
        public float proposedFirstMortgageAmount { get; set; }
        public string proposedHazardInsuranceAmount { get; set; }
        public Reoproperty[] reoProperties { get; set; }
        public Tax4506 tax4506 { get; set; }
        public float topRatioPercent { get; set; }
        public float totalAssetsAmount { get; set; }
        public float totalBaseIncomeAmount { get; set; }
        public float totalDeposit { get; set; }
        public float totalEmploymentAmount { get; set; }
        public float totalFixedPaymentAmount { get; set; }
        public float totalGrossMonthlyIncomeAmount { get; set; }
        public float totalIncomeAmount { get; set; }
        public float totalPaymentsAmount { get; set; }
        public float totalPrimaryHousingExpenseAmount { get; set; }
        public float totalUrla2020AssetsAmount { get; set; }
        public float vaSummaryTotalMonthlyGrossIncomeAmount { get; set; }
        public string borrowerPairId { get; set; }
    }

    public class Borrower
    {
        public string id { get; set; }
        public string legacyId { get; set; }
        public float adjustedAnnualIncome { get; set; }
        public int ageAtApplicationYearsCount { get; set; }
        public float annualIncome { get; set; }
        public string applicantType { get; set; }
        public string applicationTakenMethodType { get; set; }
        public bool authorizedCreditReportIndicator { get; set; }
        public string authorizedCreditReportDate { get; set; }
        public bool bankruptcyIndicator { get; set; }
        public bool bankruptcyIndicatorChapterSeven { get; set; }
        public bool bankruptcyIndicatorChapterEleven { get; set; }
        public bool bankruptcyIndicatorChapterTwelve { get; set; }
        public bool bankruptcyIndicatorChapterThirteen { get; set; }
        public float baseIncome { get; set; }
        public float baseMonthlyIncomeAmount { get; set; }
        public string birthDate { get; set; }
        public string borrowerType { get; set; }
        public string citizenshipResidencyType { get; set; }
        public float topRatioPercent { get; set; }
        public float bottomRatioPercent { get; set; }
        public float totalGrossMonthlyIncomeAmount { get; set; }
        public string creditReportAuthorizationMethod { get; set; }
        public bool currentEmploymentDoesNotApply { get; set; }
        public bool declarationsJIndicator { get; set; }
        public bool declarationsKIndicator { get; set; }
        public int dependentCount { get; set; }
        public string emailAddressText { get; set; }
        public Employment[] employment { get; set; }
        public string firstName { get; set; }
        public string firstNameWithMiddleName { get; set; }
        public string fullName { get; set; }
        public string fullNameWithSuffix { get; set; }
        public string fannieFirstName { get; set; }
        public string hmdaAge { get; set; }
        public string hmdaEthnicityReportedField1 { get; set; }
        public string hmdaEthnicityReportedFields { get; set; }
        public string hmdaEthnicityType { get; set; }
        public string hmdaGenderType { get; set; }
        public string hmdaRaceReportedField1 { get; set; }
        public string hmdaRaceReportedFields { get; set; }
        public bool hmdaWhiteIndicator { get; set; }
        public bool hmdaGendertypeMaleIndicator { get; set; }
        public bool hmdaEthnicityNotHispanicLatinoIndicator { get; set; }
        public bool homeownerPastThreeYearsIndicator { get; set; }
        public string homePhoneNumber { get; set; }
        public bool intentToOccupyIndicator { get; set; }
        public bool isBorrower { get; set; }
        public string jointAssetLiabilityReportingIndicator1 { get; set; }
        public string lastName { get; set; }
        public string lastNameWithSuffix { get; set; }
        public string maritalStatusType { get; set; }
        public string mobilePhone { get; set; }
        public bool outstandingJudgementsIndicator { get; set; }
        public bool partyToLawsuitIndicatorUrla { get; set; }
        public bool presentlyDelinquentIndicatorUrla { get; set; }
        public bool priorPropertyDeedInLieuConveyedIndicator { get; set; }
        public bool priorPropertyForeclosureCompletedIndicator { get; set; }
        public bool priorPropertyShortSaleCompletedIndicator { get; set; }
        public bool propertyProposedCleanEnergyLienIndicator { get; set; }
        public bool realEstateDoesNotApply { get; set; }
        public Residence[] residences { get; set; }
        public float subtotalLiquidAssetsMinusGiftAmount { get; set; }
        public string taxIdentificationIdentifier { get; set; }
        public float totalAssets { get; set; }
        public float totalMonthlyIncomeAmount { get; set; }
        public float totalMonthlyIncomeMinusNetRentalAmount { get; set; }
        public bool undisclosedBorrowedFundsIndicator { get; set; }
        public bool undisclosedComakerOfNoteIndicator { get; set; }
        public bool undisclosedMortgageApplicationIndicator { get; set; }
        public string urla2020CitizenshipResidencyType { get; set; }
        public float vaNetTakeHomePayAmount { get; set; }
        public float vaTotalNetIncomeAmount { get; set; }
    }

    public class Employment
    {
        public string id { get; set; }
        public string addressCity { get; set; }
        public string addressPostalCode { get; set; }
        public string addressState { get; set; }
        public string addressStreetLine1 { get; set; }
        public float basePayAmount { get; set; }
        public float bonusAmount { get; set; }
        public string countryCode { get; set; }
        public bool currentEmploymentIndicator { get; set; }
        public string employerName { get; set; }
        public string employmentStartDate { get; set; }
        public bool specialEmployerRelationshipIndicator { get; set; }
        public float monthlyIncomeAmount { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string phoneNumber { get; set; }
        public string positionDescription { get; set; }
        public string titlePhone { get; set; }
        public string urla2020StreetAddress { get; set; }
        public bool militaryEmployer { get; set; }
        public string country { get; set; }
        public string sourceOfIncomeData { get; set; }
        public bool populatedContactIndicator { get; set; }
        public string altId { get; set; }
    }

    public class Residence
    {
        public string id { get; set; }
        public string residencyType { get; set; }
        public string addressCity { get; set; }
        public string addressPostalCode { get; set; }
        public string addressState { get; set; }
        public string addressStreetLine1 { get; set; }
        public string countryCode { get; set; }
        public string county { get; set; }
        public int durationTermMonths { get; set; }
        public int durationTermYears { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string residencyBasisType { get; set; }
        public string titlePhone { get; set; }
        public string urla2020StreetAddress { get; set; }
        public string country { get; set; }
        public string altId { get; set; }
        public bool populatedContactIndicator { get; set; }
    }

    public class Coborrower
    {
        public string id { get; set; }
        public string legacyId { get; set; }
        public int ageAtApplicationYearsCount { get; set; }
        public string applicantType { get; set; }
        public string applicationTakenMethodType { get; set; }
        public bool authorizedCreditReportIndicator { get; set; }
        public string authorizedCreditReportDate { get; set; }
        public bool bankruptcyIndicator { get; set; }
        public bool bankruptcyIndicatorChapterSeven { get; set; }
        public bool bankruptcyIndicatorChapterEleven { get; set; }
        public bool bankruptcyIndicatorChapterTwelve { get; set; }
        public bool bankruptcyIndicatorChapterThirteen { get; set; }
        public string birthDate { get; set; }
        public string borrowerType { get; set; }
        public string citizenshipResidencyType { get; set; }
        public string creditReportAuthorizationMethod { get; set; }
        public bool currentEmploymentDoesNotApply { get; set; }
        public bool declarationsJIndicator { get; set; }
        public bool declarationsKIndicator { get; set; }
        public int dependentCount { get; set; }
        public string emailAddressText { get; set; }
        public Employment1[] employment { get; set; }
        public string firstName { get; set; }
        public string firstNameWithMiddleName { get; set; }
        public string fullName { get; set; }
        public string fullNameWithSuffix { get; set; }
        public string fannieFirstName { get; set; }
        public string hmda2CreditScoreForDecisionMaking { get; set; }
        public string hmda2CreditScoringModel { get; set; }
        public string hmdaAge { get; set; }
        public string hmdaCreditScoreForDecisionMaking { get; set; }
        public string hmdaCreditScoringModel { get; set; }
        public bool hmdaNoCoApplicantIndicator { get; set; }
        public bool homeownerPastThreeYearsIndicator { get; set; }
        public string homePhoneNumber { get; set; }
        public bool intentToOccupyIndicator { get; set; }
        public bool isBorrower { get; set; }
        public string lastName { get; set; }
        public string lastNameWithSuffix { get; set; }
        public string maritalStatusType { get; set; }
        public bool noCoApplicantEthnicityIndicator { get; set; }
        public bool noCoApplicantSexIndicator { get; set; }
        public bool outstandingJudgementsIndicator { get; set; }
        public bool partyToLawsuitIndicatorUrla { get; set; }
        public bool presentlyDelinquentIndicatorUrla { get; set; }
        public bool priorPropertyDeedInLieuConveyedIndicator { get; set; }
        public bool priorPropertyForeclosureCompletedIndicator { get; set; }
        public bool priorPropertyShortSaleCompletedIndicator { get; set; }
        public bool propertyProposedCleanEnergyLienIndicator { get; set; }
        public bool realEstateDoesNotApply { get; set; }
        public Residence1[] residences { get; set; }
        public string taxIdentificationIdentifier { get; set; }
        public bool undisclosedBorrowedFundsIndicator { get; set; }
        public bool undisclosedComakerOfNoteIndicator { get; set; }
        public bool undisclosedMortgageApplicationIndicator { get; set; }
        public string urla2020CitizenshipResidencyType { get; set; }
        public bool printOnAdditionalBorrowerPage {  get; set; }
    }

    public class Employment1
    {
        public string id { get; set; }
        public bool currentEmploymentIndicator { get; set; }
        public string employerName { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string titlePhone { get; set; }
        public bool militaryEmployer { get; set; }
        public string sourceOfIncomeData { get; set; }
        public bool populatedContactIndicator { get; set; }
        public string altId { get; set; }
    }

    public class Residence1
    {
        public string id { get; set; }
        public string residencyType { get; set; }
        public string addressCity { get; set; }
        public string addressPostalCode { get; set; }
        public string addressState { get; set; }
        public string addressStreetLine1 { get; set; }
        public string countryCode { get; set; }
        public string county { get; set; }
        public int durationTermMonths { get; set; }
        public int durationTermYears { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string residencyBasisType { get; set; }
        public string titlePhone { get; set; }
        public string urla2020StreetAddress { get; set; }
        public string country { get; set; }
        public string altId { get; set; }
        public bool populatedContactIndicator { get; set; }
    }

    public class Tax4506
    {
        public string formVersion { get; set; }
    }

    public class Vod
    {
        public string id { get; set; }
        public string holderName { get; set; }
        public bool includeInAusExport { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string owner { get; set; }
        public string titlePhone { get; set; }
        public float total { get; set; }
        public string sourceOfAssetData { get; set; }
        public bool populatedContactIndicator { get; set; }
        public Item[] items { get; set; }
        public string altId { get; set; }
    }

    public class Item
    {
        public int itemNumber { get; set; }
        public string type { get; set; }
        public string accountIdentifier { get; set; }
        public float urla2020CashOrMarketValueAmount { get; set; }
        public string depositoryAccountGuid { get; set; }
        public string depositoryAccountName { get; set; }
    }

    public class Income
    {
        public string incomeType { get; set; }
        public string owner { get; set; }
        public float amount { get; set; }
    }

    public class Reoproperty
    {
        public string id { get; set; }
        public string altId { get; set; }
        public string city { get; set; }
        public string countryCode { get; set; }
        public string dispositionStatusType { get; set; }
        public bool includeInAusExport { get; set; }
        public bool noLinkToDocTrackIndicator { get; set; }
        public string owner { get; set; }
        public string postalCode { get; set; }
        public bool printAttachIndicator { get; set; }
        public string propertyUsageType { get; set; }
        public string state { get; set; }
        public string streetAddress { get; set; }
        public string titlePhone { get; set; }
        public string urla2020StreetAddress { get; set; }
        public string country { get; set; }
        public string sourceOfIncomeData { get; set; }
        public bool populatedContactIndicator { get; set; }
    }

    public class Contact
    {
        public string contactType { get; set; }
        public string email { get; set; }
        public string loginId { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string postalCode { get; set; }
        public string state { get; set; }
        public string contactName { get; set; }
        public string phone { get; set; }
    }

    public class Customfield
    {
        public string fieldName { get; set; }
        public string value { get; set; }
        public string format { get; set; }
    }

    public class Fee
    {
        public string feeType { get; set; }
        public float amountPerDay { get; set; }
        public float truncatedAmountPerDay { get; set; }
        public float borPaidAmount { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public int days { get; set; }
        public float paidToOthers { get; set; }
    }

    public class Milestone
    {
        public string id { get; set; }
        public DateTime startDate { get; set; }
        public int days { get; set; }
        public bool doneIndicator { get; set; }
        public Loanassociate loanAssociate { get; set; }
        public bool reviewedIndicator { get; set; }
        public int duration { get; set; }
        public string roleRequired { get; set; }
        public string name { get; set; }
        public Milestonesetting milestoneSetting { get; set; }
    }

    public class Loanassociate
    {
        public string loanAssociateType { get; set; }
        public User user { get; set; }
        public string email { get; set; }
        public Role role { get; set; }
        public bool writeAccess { get; set; }
        public string apiClientId { get; set; }
    }

    public class User
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
    }

    public class Role
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
    }

    public class Milestonesetting
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
    }

}
