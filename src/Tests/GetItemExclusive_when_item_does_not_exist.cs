using Xunit;

namespace Tests
{
    public class GetItemExclusive_when_item_does_not_exist : GetItemExclusiveTest
    {

        protected override string SessionId
        {
            get { return "XXX"; }
        }

        [Fact]
        public void returns_null()
        {
           Assert.Null(Result); 
        }

        [Fact]
        public void not_locked()
        {
           Assert.False(Locked); 
        }


    }
}