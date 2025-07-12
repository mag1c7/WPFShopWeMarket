using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NewWpfShop.Class
{
	public class ProductDataGrid : INotifyPropertyChanged
	{
		private int _num;
		private string _nomenclature = " ";
		private decimal _quantity = 0;
		private decimal _purchasePrice = 0;
		private decimal _amount = 0;
		private string _vatPercentage = "20"; // стандартный НДС
		private decimal _vatAmount = 0;
		private decimal _totalAmount = 0;

		public int Num
		{
			get => _num;
			set
			{
				_num = value;
				OnPropertyChanged();
			}
		}

		public string Nomenclature
		{
			get => _nomenclature;
			set
			{
				_nomenclature = value;
				OnPropertyChanged();
			}
		}

		public decimal Quantity
		{
			get => _quantity;
			set
			{
				_quantity = value;
				UpdateCalculatedFields();
				OnPropertyChanged();
			}
		}

		public decimal PurchasePrice
		{
			get => _purchasePrice;
			set
			{
				_purchasePrice = value;
				UpdateCalculatedFields();
				OnPropertyChanged();
			}
		}

		public decimal Amount
		{
			get => _amount;
			set
			{
				_amount = value;
				OnPropertyChanged();
			}
		}

		public string VATPercentage
		{
			get => _vatPercentage;
			set
			{
				_vatPercentage = value;
				UpdateCalculatedFields();
				OnPropertyChanged();
			}
		}

		public decimal VATAmount
		{
			get => _vatAmount;
			set
			{
				_vatAmount = value;
				OnPropertyChanged();
			}
		}

		public decimal TotalAmount
		{
			get => _totalAmount;
			set
			{
				_totalAmount = value;
				OnPropertyChanged();
			}
		}

		private void UpdateCalculatedFields()
		{
			Amount = Quantity * PurchasePrice;

			if (decimal.TryParse(VATPercentage, out decimal vatRate))
			{
				VATAmount = Amount * (vatRate / 100);
			}

			TotalAmount = Amount + VATAmount;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
