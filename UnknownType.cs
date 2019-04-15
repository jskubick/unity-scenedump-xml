using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scenedump {
	public class UnknownType {
		private String stringValue;
		public UnknownType(String stringValue) {
			this.stringValue = stringValue;
		}

		public override String ToString() {
			return stringValue;
		}
	}
}
