(function ($){
	$(document).ready(function (){	
	
	
	
	//           ___                __       ___    __       
	//  | |\ | |  |  |  /\  |    | /__`  /\   |  | /  \ |\ | 
	//  | | \| |  |  | /~~\ |___ | .__/ /~~\  |  | \__/ | \| 
	//                                                       
	
		// Toggle button
		$('#SwitchProjecteur').bootstrapToggle();
		$('#SwitchLedSol').bootstrapToggle();
		$('#SwitchPompeMode').bootstrapToggle();
		$('#SwitchPompeManuel').bootstrapToggle();
		
		
		// Chart
		moment.locale('fr-ch');
				
		Chart.defaults.global.defaultColor = '#fff';
		Chart.defaults.global.defaultFontColor = '#fff';
		Chart.defaults.global.legend.display = false;
        Chart.defaults.global.tooltips.enabled = false;

		var ctx = document.getElementById("graphTemperatures").getContext('2d');
		var temperaturesChart = new Chart(ctx, {
			type: 'line',
			data: {
				datasets: [{
					label: "Eau",
					backgroundColor: 'rgba(0,0,255,0.1)',
                    borderColor: 'rgba(0,0,255,1)',
                    pointRadius: 0,
					data: []
				}, {
					label: "Air",
					backgroundColor: 'rgba(0,255,0,0.1)',
                    borderColor: 'rgba(0,255,0,1)',
                    pointRadius: 0,
					data: []
				},
				]
			},
			options: {
				scales: {
					xAxes: [
					{
						type: "time",
						time: {
							unit: 'second',
							displayFormats: {
								'second': 'H:mm', // 11:20
                            },
                        },
						scaleLabel: {
							display: false,
							labelString: 'Temps',
						},
                        ticks: {
                            autoSkip: true,
                            autoSkipPadding: 30,
                        }
					}],
					yAxes: [{
						type: 'linear',
                        ticks: {
                            suggestedMin: 10,
                            suggestedMax: 40,
							callback: function(label, index, labels) {
								return label + '°';
							}
						},
						scaleLabel: {
							display: false,
							labelString: 'Temperature [°C]'
						}
					}]
				}
			}
		});

        // Drum
        $(".GrantSelect").drum({
            panelCount: 10,
        });



	//        __   __   __   __       
	//  |  | /  \ /  \ |__) /__`  /\  
	//  |/\| \__/ \__/ |    .__/ /~~\ 
	//                                

        woopsa = new WoopsaClient("http://localhost:10001/woopsa", jQuery);
        woopsa.username = "anonyme";
        woopsa.password = "nopass";

        woopsa.onError(function (type, errorThown) {
            $('#ErrorModal').modal('show');
            console.log((new Date().toUTCString()) + ": " + type + " - " + errorThown);
            $(".log").prepend("<b>" + (new Date().toUTCString()) + ": </b>" + type + " - " + errorThown + "<br>");
        });


        function GetPrivilegeCode() {
            return $('#CheckSelect1').val() + $('#CheckSelect2').val() + $('#CheckSelect3').val();
        }

        function SetPrivilegeCode(code) { // TODO
            code = Number(code);
            $("#CheckSelect1").drum('setIndex', code % 100);
            code = code - (code % 100) * 100;
            $("#CheckSelect2").drum('setIndex', code % 10);
            code = code - (code % 10) * 10;
            $("#CheckSelect3").drum('setIndex', code);
        }

        var HasPrivilege = false;

        // Privilege
        function CheckPrivilege(canGrantPrivilegeAcess = false) {
            console.log('CheckPrivilege');

            woopsa.invoke("/CanControl", { 'privilegeCode': GetPrivilegeCode() }, function (value) {
                console.log('Cancontrol ' + value);

                if (value)
                {
                    $('#CheckPrivilegeModal').modal('hide'); // Hide drums
                }
               

                $('#PrivilegeIsMine').toggle(value && !canGrantPrivilegeAcess);
                $('#PrivilegeIsLock').toggle(!(value && !canGrantPrivilegeAcess));

                $('#PrivilegeCode').toggle(value && !canGrantPrivilegeAcess);
                $('#PrivilegeCodeValue').html(GetPrivilegeCode());

                HasPrivilege = value;

                //$('#ControlPanel').toggle(value); // Disable/Enable control panel
                $('#ControlPanelFieldset').prop('disabled', !value);
            });
        }

        $("select.CheckSelect").drum({
            panelCount: 10,
            /*onChange: function (e) {
                CheckPrivilege();
            }*/
        });

        // Storage

        if (localStorage.CheckPin) {
            SetPrivilegeCode(localStorage.CheckPin);
        } else {
            localStorage.CheckPin = GetPrivilegeCode();
        }


        $('#CheckPrivilegeModalSubmit').click(function () {
            CheckPrivilege();
        });

        // Ask to privilige

        var subscriptionCanGrantPrivilegeAcess = null;
        woopsa.onChange("CanGrantPrivilegeAccess", function (value) {
            console.log("Received notification CanGrantPrivilegeAccess, new value = " + value);

            // value = true : unlocked
            $('#PrivilegeIsTaken').toggle(!value);
            $('#PrivilegeIsFree').toggle(value);

            

            CheckPrivilege(value);

        },/*monitorInterval*/0.08,/*publishInterval*/0.08,
        function (subscription) {
            subscriptionCanGrantPrivilegeAcess = subscription;
        });

        

        $('#GrantPrivilegeModalSubmit').click(function () {
            console.log($('#inputPseudo').val());
            var privilegeCode = $('#GrantSelect1').val() + $('#GrantSelect2').val() + $('#GrantSelect3').val();
            console.log(privilegeCode);
            woopsa.invoke("/GrantPrivilegeAccess", { 'pseudo': $('#inputPseudo').val(), 'privilegeCode': privilegeCode },
            function (value) {
                console.log("GrantPrivilegeAccess");

                $("#CheckSelect1").drum('setIndex', $('#GrantSelect1').val()); 
                $("#CheckSelect2").drum('setIndex', $('#GrantSelect2').val()); 
                $("#CheckSelect3").drum('setIndex', $('#GrantSelect3').val()); 

                $('#GrantPrivilegeModal').modal('hide');
            });
        });

        $('#PrivilegeRelease').click(function () {
            woopsa.invoke("/ReleasePrivilegeAccess", { 'privilegeCode': GetPrivilegeCode() }, function () {
                console.log("ReleasePrivilegeAccess");
            });
        });
			
	//   __          __          __   __  
	//  |__) | |\ | |  \ | |\ | / _` /__` 
	//  |__) | | \| |__/ | | \| \__> .__/ 
	//                                    

        var subscriptionPseudo = null;
        woopsa.onChange("Pseudo", function (value) {
            console.log("Received notification Pseudo, new value = " + value);
            $('#PrivilegePseudo').html(value);
        },/*monitorInterval*/0.08,/*publishInterval*/0.08,
        function (subscription) {
            subscriptionPseudo = subscription;
        });

        var subscriptionTimeout = null;
        woopsa.onChange("DateTimeout", function (value) {
            //console.log("Received notification Privilege Timeout, new value = " + value);
            $('#PrivilegeDateTimeout').html(new Date(value).toLocaleTimeString());
        },/*monitorInterval*/0.08,/*publishInterval*/0.08,
        function (subscription) {
            subscriptionTimeout = subscription;
        });

		var subscriptionTemperatureEau = null;
		woopsa.onChange("TemperatureEau", function (value){
			//console.log("Received notification TemperatureEau, new value = " + value);
            $('#TemperatureEau').html(value.toFixed(1) + '&nbsp;°C');
        },/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionTemperatureEau = subscription;
		});
			
		var subscriptionHistoriqueTemperatureEau = null;
		woopsa.onChange("HistoriqueTemperatureEauSerialized", function (value){
			//console.log("Received notification HistoriqueTemperatureEauSerialized, new value = " + value);
				
			data = JSON.parse(value);
			temperaturesChart.data.datasets[0].data = data;
			temperaturesChart.update();
				
        },/*monitorInterval*/0.5,/*publishInterval*/0.5, 
		function (subscription){
			subscriptionHistoriqueTemperatureEau = subscription;
		});
			
		var subscriptionTemperatureAir = null;
		woopsa.onChange("TemperatureAir", function (value){
			//console.log("Received notification TemperatureAir, new value = " + value);
            $('#TemperatureAir').html(value.toFixed(1) + '&nbsp;°C');
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionTemperatureAir = subscription;
		});
			
		var subscriptionHistoriqueTemperatureAir = null;
		woopsa.onChange("HistoriqueTemperatureAirSerialized", function (value){
			//console.log("Received notification HistoriqueTemperatureAirSerialized, new value = " + value);
				
			data = JSON.parse(value);
			temperaturesChart.data.datasets[1].data = data;
			temperaturesChart.update();
				
        },/*monitorInterval*/0.5,/*publishInterval*/0.5, 
		function (subscription){
			subscriptionHistoriqueTemperatureAir = subscription;
		});
			
		// Lumières
			
		var subscriptionLumiereSol = null;
		woopsa.onChange("LumiereSol", function (value){
			console.log("Received notification LumiereSol, new value = " + value);
			if(value)
			{
				$('#SwitchLedSol').bootstrapToggle('on');
			} else
			{
				$('#SwitchLedSol').bootstrapToggle('off');
			}
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionLumiereSol = subscription;
		});
			
        $('#SwitchLedSol').change(function () {
            if (HasPrivilege)
                woopsa.invoke("ControlLightFloor", { privilegeCode : GetPrivilegeCode(), state: this.checked }, function (response){ });
				
			if(this.checked) {
				console.log('LedSol Checked');
			}
			else{
				console.log('LedSol Not Checked');
			}
		});
			
		var subscriptionLumiereProjecteur = null;
		woopsa.onChange("Projecteur", function (value){
			console.log("Received notification Projecteur, new value = " + value);
			if(value)
			{
				$('#SwitchProjecteur').bootstrapToggle('on');
			} else
			{
				$('#SwitchProjecteur').bootstrapToggle('off');
			}
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionLumiereProjecteur = subscription;
		});
        
        $('#SwitchProjecteur').change(function () {
            if (HasPrivilege)
                woopsa.invoke("ControlLightMain", { privilegeCode : GetPrivilegeCode(), state: this.checked }, function (response){ });

			if(this.checked) {
				console.log('Projecteur Checked');
			}
			else{
				console.log('Projecteur Not Checked');
			}
		});
				
		// Pompe
			
		var subscriptionPompeMode = null;
		woopsa.onChange("PompeMode", function (value){
			console.log("Received notification PompeMode, new value = " + value);
			if(value)
			{
				$('#SwitchPompeMode').bootstrapToggle('on');
			} else
			{
				$('#SwitchPompeMode').bootstrapToggle('off');
			}
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionPompeMode = subscription;
		});
			
		$('#SwitchPompeMode').change(function() {
            if (HasPrivilege)
                woopsa.invoke("ControlPumpMode", { privilegeCode : GetPrivilegeCode(), state: this.checked }, function (response){ });

            $('#SwitchPompeManuelFieldset').prop('disabled', this.checked);

			if(this.checked) {
				console.log('PompeMode Checked');
			}
			else{
                console.log('PompeMode Not Checked');
			}
		});
			
		var subscriptionPompeManuel = null;
		woopsa.onChange("PompeManuel", function (value){
			console.log("Received notification PompeManuel, new value = " + value);
			if(value)
			{
				$('#SwitchPompeManuel').bootstrapToggle('on');
			} else
			{
				$('#SwitchPompeManuel').bootstrapToggle('off');
			}
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionPompeManuel = subscription;
        }); 

		$('#SwitchPompeManuel').change(function() {
            if (HasPrivilege)
                woopsa.invoke("ControlPumpManual", { privilegeCode : GetPrivilegeCode(), state: this.checked }, function (response){ });
				
			if(this.checked) {
				console.log('PompeManuel Checked');
			}
			else{
				console.log('PompeManuel Not Checked');
			}
		});
			
		var subscriptionTempsActivation = null;
		woopsa.onChange("TempsActivation", function (value){
			console.log("Received notification TempsActivation, new value = " + value);
			$('#InputPompeTempsActivation').val(value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionTempsActivation = subscription;
		});
			
		$('#InputPompeTempsActivation').change(function() {
			if($( this ).val() != '') {
				woopsa.write("/TempsActivation", $( this ).val(), function (response){
					if ( response == true ){
					console.log("The value was written successfully!");
					} else {
					console.log("The value was not written successfully :(");
					}
				});
					
				console.log('TempsActivation change = ' + $( this ).val());
			}
		});
			
		var subscriptionTempsCycle = null;
		woopsa.onChange("TempsCycle", function (value){
			console.log("Received notification TempsCycle, new value = " + value);
			$('#InputPompeTempsCycle').val(value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionTempsCycle = subscription;
		});
			
		$('#InputPompeTempsCycle').change(function() {
			if($( this ).val() != ''){
				woopsa.write("/TempsCycle", $( this ).val(), function (response){
					if ( response == true ){
					console.log("The value was written successfully!");
					} else {
					console.log("The value was not written successfully :(");
					}
				}); 
				console.log('TempsCycle change = ' + $( this ).val());
			}
		});
	});
})(jQuery);
//temperaturesChart.data.datasets[1].data