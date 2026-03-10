using LegoSetNotifier.RebrickableData;

namespace LegoSetNotifier.Test
{
    [TestClass]
    public class RebrickableDataTests
    {
        [TestMethod]
        public void TestLegoSetIsPurchaseableSetTrue()
        {
            var testLegoSet = new LegoSet
            {
                // Purchaseable set: 40748-1 "Batman 8in1 Figure"
                // https://rebrickable.com/sets/40748-1
                // https://www.lego.com/en-us/product/40748
                ExtendedSetNumber = "40748-1",
            };

            Assert.IsTrue(testLegoSet.IsPurchaseableSet());
        }

        [TestMethod]
        public void TestLegoSetIsPurchaseableSetFalseNotEnoughDigits()
        {
            var testLegoSet = new LegoSet
            {
                // Not a purchaseable set: 6858-1 "Catwoman Catcycle City Chase"
                // https://rebrickable.com/sets/6858-1
                ExtendedSetNumber = "6858-1",
            };

            Assert.IsFalse(testLegoSet.IsPurchaseableSet());
        }

        [TestMethod]
        public void TestLegoSetIsPurchaseableSetFalseTooManyDigits()
        {
            var testLegoSet = new LegoSet
            {
                // Not a purchaseable set: 40501735-1 "Batman Lunch Box"
                // https://rebrickable.com/sets/40501735-1
                ExtendedSetNumber = "40501735-1",
            };

            Assert.IsFalse(testLegoSet.IsPurchaseableSet());
        }

        [TestMethod]
        public void TestLegoSetIsPurchaseableSetFalseNonDigits()
        {
            var testLegoSet = new LegoSet
            {
                // Not a purchaseable set: COMIC-3 "Build Your Own Batman Comic Book"
                // https://rebrickable.com/sets/COMIC-3
                ExtendedSetNumber = "COMIC-3",
            };

            Assert.IsFalse(testLegoSet.IsPurchaseableSet());
        }
    }
}
