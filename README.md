DokStore
========

An attempt to implement a simple document database on top of a SQL database (using NHibernate as the persistance layer).

The concepts
-------------
A database makes it easy to store complex objects (typically aggregate roots) without the ORM impendance mismatch. 
DokStore stores object graphs serialized as json strings in a SQL database. Objects of the same class are organized in collections, and each collection is persisted to a matching table in the database.

In order to facilitate searching in the collection additional columns can be added to the collection table. 
The collection class get to set the index columns in a callback method when an entity is persisted.

Usage
----------

Taking this example entity class:

	class Customer
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Phone { get; set; } 
		public string Email { get; set; }
		public Address DeliveryAddress { get; set; }
	}
	
	class Address
	{
		public string Street { get; set; }
		public string City { get; set; }
		public int Number { get; set; }
		public string PostalCode { get; set; }
	}
	
To persist it in a collection we write a matching collection class:

	class Customers : DocumentCollection<Customer>
	{
		//Id is neccesary
		public int Id { get; set; }
		
		//These are the index fields. 
		//We can choose which properties in the entity we wish to index and also create computed indices
		public string Name { get; set; }
		public string Phone { get; set; }
		public string Email { get; set; }
		public string Street { get; set; }
		public string City { get; set; }
		public string PostalCode { get; set; }
		public bool HasAddress { get; set; }

		//Callback method to set index fields before persisting entity
        protected override void UpdateIndices(Customer customer)
        {
            Name = customer.Name;
            Phone = customer.Phone;
            Email = customer.Email;

            HasAddress = customer.DeliveryAddress != null;	//this is an example of a computed index
            if (HasAddress)
            {
                Street = customer.DeliveryAddress.Street;	//index fields retrieved from component object
                City = customer.DeliveryAddress.Street;
                PostalCode = customer.DeliveryAddress.PostalCode;
            }
            else
            {
                Street = "";
                City = "";
                PostalCode = "";
            }
        }
	}

The database table to handle this collection would need the following layout:
	create table Customers (
		 Id int not null identity,
		 JsonDocument varchar(max) not null,	--serialized entity object
		 Name varchar,
		 Phone varchar,
		 Email varchar,
		 Street varchar,
		 PostalCode varchar,
		 HasAddress bit
	)