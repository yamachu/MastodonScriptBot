MSStream.OfType<Status>()
.Subscribe(x => Console.WriteLine($"{x.Account.FullUserName}: {x.Content}"))