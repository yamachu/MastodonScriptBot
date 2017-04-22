MSStream.OfType<Status>()
.Where(x => x.Content.Contains("マストドン"))
.Subscribe(x => 
{
    System.Console.WriteLine(x.Content);
    Client.Favourite(x.Id);
})