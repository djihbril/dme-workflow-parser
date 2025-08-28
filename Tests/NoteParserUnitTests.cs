using dme_workflow_parser.Models;
using dme_workflow_parser.Services;

namespace dme_workflow_parser.Tests
{
    [Trait("Category", "Note parsing")]
    [Trait("Scenario", "Successfully parsing text physician notes")]
    public class WhenSuccessfullyParsingTextPhysicianNotes
    {
        readonly NoteParser parser;

        public WhenSuccessfullyParsingTextPhysicianNotes()
        {
            Settings settings = new()
            {
                InputFolder = "C:\\projects\\dotnet\\dme-workflow-parser\\Tests\\Input",
                TextInputFile = "test_physician_note.txt"
            };

            parser = new(settings);

        }

        [Fact]
        public void ShouldNotReturnAnError()
        {
            (string? err, Order? order) = parser.Parse();

            Assert.Null(err);
            Assert.NotNull(order);
        }

        [Fact]
        public void ShouldReturnTheExpectedOrder()
        {
            Order expectedOrder = new()
            {
                Device = "Oxygen Tank",
                OrderingProvider = "Dr. Cuddy",
                Liters = "2 L",
                Usage = "sleep and exertion",
                Diagnosis = "COPD",
                PatientName = "Harold Finch",
                PatientDob = "04/12/1952",
                MaskType = "nasal",
                AddOns = ["Humidifier"]
            };
            Order? actualOrder = parser.Parse().order;

            Assert.NotNull(actualOrder);
            Assert.Equal(expectedOrder.Device, actualOrder.Device, true);
            Assert.Equal(expectedOrder.OrderingProvider, actualOrder.OrderingProvider, true);
            Assert.Equal(expectedOrder.Liters, actualOrder.Liters, true);
            Assert.Equal(expectedOrder.Usage, actualOrder.Usage, true);
            Assert.Equal(expectedOrder.Diagnosis, actualOrder.Diagnosis, true);
            Assert.Equal(expectedOrder.PatientName, actualOrder.PatientName, true);
            Assert.Equal(expectedOrder.PatientDob, actualOrder.PatientDob, true);
            Assert.Equal(expectedOrder.MaskType, actualOrder.MaskType, true);

            Assert.NotNull(actualOrder.AddOns);
            Assert.Equal(expectedOrder.AddOns.Count, actualOrder.AddOns.Count);
            expectedOrder.AddOns.ForEach(addOn => Assert.Contains(addOn, actualOrder.AddOns, StringComparer.OrdinalIgnoreCase));

            Assert.Null(actualOrder.Qualifier);
            Assert.Equal(expectedOrder.Qualifier, actualOrder.Qualifier);
        }
    }

    [Trait("Category", "Note parsing")]
    [Trait("Scenario", "Unsuccessfully parsing text physician notes")]
    public class WhenUnsuccessfullyParsingTextPhysicianNotes
    {
        readonly NoteParser parser;

        public WhenUnsuccessfullyParsingTextPhysicianNotes()
        {
            Settings settings = new()
            {
                InputFolder = "C:\\projects\\dotnet\\dme-workflow-parser\\Tests\\Input",
                TextInputFile = "non_existent_file.txt"
            };

            parser = new(settings);
        }

        [Fact]
        public void ShouldReturnAnError()
        {
            (string? err, Order? order) = parser.Parse();

            Assert.NotNull(err);
            Assert.Null(order);
        }
    }

    [Trait("Category", "Note parsing")]
    [Trait("Scenario", "Successfully parsing json physician notes")]
    public class WhenSuccessfullyParsingJsonPhysicianNotes
    {
        readonly NoteParser parser;

        public WhenSuccessfullyParsingJsonPhysicianNotes()
        {
            Settings settings = new()
            {
                InputFolder = "C:\\projects\\dotnet\\dme-workflow-parser\\Tests\\Input",
                JsonInputFile = "test_physician_note.json"
            };

            parser = new(settings);

        }

        [Fact]
        public void ShouldNotReturnAnError()
        {
            (string? err, Order? order) = parser.Parse();

            Assert.Null(err);
            Assert.NotNull(order);
        }

        [Fact]
        public void ShouldReturnTheExpectedOrder()
        {
            Order expectedOrder = new()
            {
                Device = "Oxygen Tank",
                OrderingProvider = "Dr. Cuddy",
                Liters = "2 L",
                Usage = "sleep and exertion",
                Diagnosis = "COPD",
                PatientName = "Harold Finch",
                PatientDob = "04/12/1952",
                MaskType = "nasal",
                AddOns = ["Humidifier"]
            };
            Order? actualOrder = parser.Parse(true).order;

            Assert.NotNull(actualOrder);
            Assert.Equal(expectedOrder.Device, actualOrder.Device, true);
            Assert.Equal(expectedOrder.OrderingProvider, actualOrder.OrderingProvider, true);
            Assert.Equal(expectedOrder.Liters, actualOrder.Liters, true);
            Assert.Equal(expectedOrder.Usage, actualOrder.Usage, true);
            Assert.Equal(expectedOrder.Diagnosis, actualOrder.Diagnosis, true);
            Assert.Equal(expectedOrder.PatientName, actualOrder.PatientName, true);
            Assert.Equal(expectedOrder.PatientDob, actualOrder.PatientDob, true);
            Assert.Equal(expectedOrder.MaskType, actualOrder.MaskType, true);

            Assert.NotNull(actualOrder.AddOns);
            Assert.Equal(expectedOrder.AddOns.Count, actualOrder.AddOns.Count);
            expectedOrder.AddOns.ForEach(addOn => Assert.Contains(addOn, actualOrder.AddOns, StringComparer.OrdinalIgnoreCase));

            Assert.Null(actualOrder.Qualifier);
            Assert.Equal(expectedOrder.Qualifier, actualOrder.Qualifier);
        }
    }

    [Trait("Category", "Note parsing")]
    [Trait("Scenario", "Unsuccessfully parsing json physician notes")]
    public class WhenUnsuccessfullyParsingJsonPhysicianNotes
    {
        readonly NoteParser parser;

        public WhenUnsuccessfullyParsingJsonPhysicianNotes()
        {
            Settings settings = new()
            {
                InputFolder = "C:\\projects\\dotnet\\dme-workflow-parser\\Tests\\Input",
                JsonInputFile = "non_existent_file.json"
            };

            parser = new(settings);
        }

        [Fact]
        public void ShouldReturnAnError()
        {
            (string? err, Order? order) = parser.Parse(true);

            Assert.NotNull(err);
            Assert.Null(order);
        }
    }
}