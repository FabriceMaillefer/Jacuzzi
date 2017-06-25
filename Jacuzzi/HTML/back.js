(function ($){
	$(document).ready(function (){	
	
	
	
	//           ___                __       ___    __       
	//  | |\ | |  |  |  /\  |    | /__`  /\   |  | /  \ |\ | 
	//  | | \| |  |  | /~~\ |___ | .__/ /~~\  |  | \__/ | \| 
	//                                                       
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

        function SetPrivilegeCode(code) {
            code = Number(code);
            $("#CheckSelect3").drum('setIndex', code % 10);
            code = (code - (code % 10)) / 10;
            $("#CheckSelect2").drum('setIndex', code % 10);
            code = (code - (code % 10)) / 10;
            $("#CheckSelect1").drum('setIndex', code % 10); 
        }

        var HasPrivilege = false;

        function ToggleButton(button, mode)
        {
            button.removeClass('btn-default');
            if (mode)
            {
                button.removeClass('btn-danger');
                button.addClass('btn-success');
            }
            else
            {
                button.addClass('btn-danger');
                button.removeClass('btn-success');
            }
        }

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
            localStorage.CheckPin = privilegeCode;
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
            ToggleButton($('#SwitchLedSol'), value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionLumiereSol = subscription;
		});

        $('#SwitchLedSol').click(function () {
            if (HasPrivilege)
            {
                woopsa.invoke("ToggleLightFloor", { privilegeCode: GetPrivilegeCode() }, function (response) { });
            }
		});
			
		var subscriptionLumiereProjecteur = null;
		woopsa.onChange("Projecteur", function (value){
            ToggleButton($('#SwitchProjecteur'), value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionLumiereProjecteur = subscription;
		});
        
        $('#SwitchProjecteur').click(function () {
            if (HasPrivilege)
                woopsa.invoke("ToggleLightMain", { privilegeCode : GetPrivilegeCode() }, function (response){ });
            
		});
				
		// Pompe
			
		var subscriptionPompeMode = null;
        woopsa.onChange("PompeMode", function (value) {
            $('#SwitchPompeModeManuel').toggle(!value);
            $('#SwitchPompeModeAuto').toggle(value);
            ToggleButton($('#SwitchPompeMode'), value);
            $('#SwitchPompeManuel').prop('disabled', value);
            $('#SwitchPompeManuelModeAuto').toggle(value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionPompeMode = subscription;
		});
			
        $('#SwitchPompeMode').click(function() {
            if (HasPrivilege)
                woopsa.invoke("TogglePumpMode", { privilegeCode: GetPrivilegeCode() }, function (response) { });
        });
			
		var subscriptionPompeManuel = null;
        woopsa.onChange("PompeManuel", function (value) {
            ToggleButton($('#SwitchPompeManuel'), value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
			subscriptionPompeManuel = subscription;
        }); 

        $('#SwitchPompeManuel').click(function() {
            if (HasPrivilege)
                woopsa.invoke("TogglePumpManual", { privilegeCode : GetPrivilegeCode()}, function (response){ });
		});

        $('#SwitchExtinction').click(function () {
            if (HasPrivilege)
                woopsa.invoke("Extinction", { privilegeCode: GetPrivilegeCode() }, function (response) { });
        });

		var subscriptionTempsActivation = null;
		woopsa.onChange("TempsActivation", function (value){
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

        var subscriptionPompeTempsRestant = null;
        woopsa.onChange("TempsRestantPompe", function (value) {
            $('#PompeTempsRestant').html(value);
        },/*monitorInterval*/0.08,/*publishInterval*/0.08,
            function (subscription) {
                subscriptionPompeTempsRestant = subscription;
            });

        var subscriptionTempsDesactivation = null;
		woopsa.onChange("TempsCycle", function (value){
            $('#InputPompeTempsDesactivation').val(value);
		},/*monitorInterval*/0.08,/*publishInterval*/0.08, 
		function (subscription){
            subscriptionTempsDesactivation = subscription;
		});
			
        $('#InputPompeTempsDesactivation').change(function() {
			if($( this ).val() != ''){
                woopsa.write("/TempsDesactivation", $( this ).val(), function (response){
					if ( response == true ){
					console.log("The value was written successfully!");
					} else {
					console.log("The value was not written successfully :(");
					}
				}); 
                console.log('TempsDesactivation change = ' + $( this ).val());
			}
        });
	});
})(jQuery);
//temperaturesChart.data.datasets[1].data